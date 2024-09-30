
// see: https://developer.spotify.com/documentation/web-api
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using System.Web;
using Microsoft.Extensions.Logging;

namespace Spotify
{
    public class ApiClient : IDisposable
    {
        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            WriteIndented = true
        };

        const string defaultCredentialsFilepath = ".credentials";
        const string cacheFilepath = ".cache";

        private readonly ILogger? logger;

        private string? clientId;
        private HttpClient? apiHttpClient;
        private HttpClient? accountsHttpClient;

        private AccessTokenResponse? accessTokenResponse;
        private DateTime accessTokenExpiration;
        readonly TimeSpan accessTokenExpirationMargin = TimeSpan.FromSeconds(30);

        private Cache Cache = new();

        public ApiClient(ILoggerFactory? loggerFactory = null)
        {
            logger = loggerFactory?.CreateLogger<ApiClient>();
        }

        public bool IsAuthenticated { get; private set; }

        public void Dispose()
        {
            apiHttpClient.Dispose();
            accountsHttpClient.Dispose();
        }

        public static async Task<Credentials?> LoadCredentials(ILogger? logger = null)
        {
            string? clientId = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID");
            string? clientSecret = Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET");

            if (clientId != null && clientSecret != null)
            {
                logger?.LogInformation("Credentials loaded from environment variables 'SPOTIFY_CLIENT_ID' and 'SPOTIFY_CLIENT_SECRET'");

                return new Credentials
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                };
            }

            var credentialsFilepath = Environment.GetEnvironmentVariable("SPOTIFY_CREDENTIALS_PATH") ?? defaultCredentialsFilepath;

            if (!File.Exists(credentialsFilepath))
            {
                logger?.LogWarning($"Failed to load credentials. Environment variables 'SPOTIFY_CLIENT_ID' and 'SPOTIFY_CLIENT_SECRET' are not set and credential file '{credentialsFilepath}' does not exist.");
                return null;
            }

            try
            {
                using var credentialsFile = File.OpenRead(credentialsFilepath);
                var credentials = await JsonSerializer.DeserializeAsync<Credentials>(credentialsFile);
                logger?.LogInformation($"Credentials loaded from file '{credentialsFilepath}'");
                return credentials;
            }
            catch (Exception exception)
            {
                logger?.LogError(exception, $"Failed to load credentials from file '{credentialsFilepath}' due to an error.");
                return null;
            }
        }

        public async Task LoadCache()
        {
            if (!File.Exists(cacheFilepath))
            {
                logger?.LogInformation($"Cache file '{cacheFilepath}' does not exist.");
                return;
            }

            try
            {
                using var cacheFile = File.OpenRead(cacheFilepath);
                Cache = await JsonSerializer.DeserializeAsync<Cache>(cacheFile);
                logger?.LogInformation($"Cache loaded from file '{cacheFilepath}'.");
            }
            catch (Exception exception)
            {
                logger?.LogError(exception, $"Failed to load cache from file '{cacheFilepath}' due to an error.");
            }
        }

        public async Task SaveCache()
        {
            using var cacheFile = File.OpenWrite(cacheFilepath);
            await JsonSerializer.SerializeAsync(cacheFile, Cache, jsonSerializerOptions);
            cacheFile.SetLength(cacheFile.Position);
        }

        public async Task<bool> Authenticate()
        {
            if (IsAuthenticated) return true;

            var credentials = await LoadCredentials(logger);

            if (credentials == null)
            {
                logger?.LogWarning("Spotify credentials cannot be loaded.");
                return false;
            }

            return await Authenticate(credentials.ClientId!, credentials.ClientSecret!);
        }

        public async Task<bool> Authenticate(string clientId, string clientSecret)
        {
            if (IsAuthenticated) return true;

            await LoadCache();

            this.clientId = clientId;
            var basicCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            accountsHttpClient = new HttpClient
            {
                BaseAddress = new Uri("https://accounts.spotify.com/api/")
            };
            accountsHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicCredentials);
            accountsHttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            apiHttpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.spotify.com/v1/")
            };
            apiHttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (Cache.RefreshToken != null && await RefreshAccessToken())
            {
                IsAuthenticated = true;
                return true;
            }

            var redirectUri = "http://localhost:8080/auth";
            var authorizeParameters = new Dictionary<string, string> {
                {"response_type",  "code" },
                {"client_id",  clientId },
                {"scope",  "user-read-playback-state user-modify-playback-state" },
                {"redirect_uri", redirectUri },
                {"state", Guid.NewGuid().ToString() },
            };
            var authorizeQueryString = string.Join('&', authorizeParameters.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
            var authorizeUrl = $"https://accounts.spotify.com/authorize?{authorizeQueryString}";

            using var httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:8080/");
            httpListener.Start();
            Console.WriteLine($"Authenticate at:\n{authorizeUrl}");

            var context = await httpListener.GetContextAsync();
            var userAuthorization = HttpUtility.ParseQueryString(context.Request.Url.Query);
            var authorizationCode = userAuthorization.Get("code");

            HttpListenerResponse response = context.Response;
            await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("<html><body>OK</body></html>"));
            response.OutputStream.Close();

            httpListener.Stop();

            var authResponse = await accountsHttpClient.PostAsync("token", new FormUrlEncodedContent(new Dictionary<string, string> {
                { "grant_type", "authorization_code" },
                { "code", authorizationCode },
                { "redirect_uri", redirectUri },
            }));
            accessTokenResponse = await authResponse.Content.ReadFromJsonAsync<Spotify.AccessTokenResponse>();
            accessTokenExpiration = DateTime.UtcNow.AddSeconds(accessTokenResponse.ExpiresIn);

            apiHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(accessTokenResponse.TokenType, accessTokenResponse.AccessToken);

            Cache.RefreshToken = accessTokenResponse.RefreshToken;
            await SaveCache();

            IsAuthenticated = true;
            return true;
        }

        public async Task RefreshAccessTokenIfExpired()
        {
            if (accessTokenExpiration > DateTime.UtcNow) return;
            await RefreshAccessToken();
        }

        public async Task<bool> RefreshAccessToken()
        {
            if (Cache.RefreshToken is null) return false;

            var response = await accountsHttpClient.PostAsync("token", new FormUrlEncodedContent(new Dictionary<string, string> {
                {"grant_type",  "refresh_token" },
                {"refresh_token", Cache.RefreshToken },
                {"client_id",  clientId },
            }));

            if (!response.IsSuccessStatusCode)
            {
                logger?.LogWarning($"Unable to refresh access token.");
                Cache.RefreshToken = null;
                return false;
            }

            accessTokenResponse = await response.Content.ReadFromJsonAsync<Spotify.AccessTokenResponse>();
            accessTokenExpiration = DateTime.UtcNow.AddSeconds(accessTokenResponse.ExpiresIn).Subtract(accessTokenExpirationMargin);

            apiHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(accessTokenResponse.TokenType, accessTokenResponse.AccessToken);

            if (accessTokenResponse.RefreshToken is not null)
            {
                Cache.RefreshToken = accessTokenResponse.RefreshToken;
                await SaveCache();
                logger?.LogInformation($"Access token refreshed.");
            }

            return true;
        }

        // see: https://developer.spotify.com/documentation/web-api/reference/get-the-users-currently-playing-track
        public async Task<CurrentlyPlayingResponse?> GetCurrentlyPlayingTrack()
        {
            await RefreshAccessTokenIfExpired();

            var response = await apiHttpClient.GetAsync("me/player/currently-playing");

            if (response.StatusCode == HttpStatusCode.NoContent) return null;

            var currentlyPlaying = await response.Content.ReadFromJsonAsync<CurrentlyPlayingResponse>();
            if (currentlyPlaying is null) return null;

            var deviceId = currentlyPlaying.Device.Id;

            if (deviceId != Cache.DeviceId)
            {
                Cache.DeviceId = deviceId;
                await SaveCache();
            }

            return currentlyPlaying;
        }

        // see: https://developer.spotify.com/documentation/web-api/reference/get-information-about-the-users-current-playback
        public async Task<PlaybackState?> GetPlaybackState()
        {
            await RefreshAccessTokenIfExpired();

            var response = await apiHttpClient.GetAsync("me/player");

            if (response.StatusCode == HttpStatusCode.NoContent) return null;

            var playerState = await response.Content.ReadFromJsonAsync<PlaybackState>();
            if (playerState is null) return null;

            var device = playerState.Device;

            if (device.Id is not null)
            {
                if (Cache.PreferredDeviceName is null)
                {
                    Cache.PreferredDeviceName = device.Name;
                    Cache.DeviceId = device.Id;
                    await SaveCache();
                }
                else if (Cache.PreferredDeviceName == device.Name && Cache.DeviceId != device.Id)
                {
                    Cache.DeviceId = device.Id;
                    await SaveCache();
                }
            }

            return playerState;
        }

        public async Task<List<Device>?> GetDevices()
        {
            await RefreshAccessTokenIfExpired();
            var response = await apiHttpClient.GetFromJsonAsync<DevicesResponse>("me/player/devices");

            var preferredDevice = response?.Devices?.FirstOrDefault(d => d.Name == Cache.PreferredDeviceName);

            if (preferredDevice?.Id is not null && preferredDevice.Id != Cache.DeviceId)
            {
                Cache.DeviceId = preferredDevice.Id;
                await SaveCache();
            }

            return response?.Devices;
        }

        public async Task<List<Track>?> GetArtistTopTracks(string artistId)
        {
            await RefreshAccessTokenIfExpired();

            var url = $"artists/{artistId}/top-tracks" + Helpers.CreateUrlQueryString(
                "market", "FI");

            var response = await apiHttpClient.GetFromJsonAsync<TopTracksResponse>(url);
            return response?.Tracks;
        }

        public async Task<ListPage<TrackSimple>?> GetAlbumTracks(string albumId)
        {
            await RefreshAccessTokenIfExpired();
            return await apiHttpClient.GetFromJsonAsync<ListPage<TrackSimple>>($"albums/{albumId}/tracks");
        }

        public async Task<SearchResponse?> Search(string query, string type, int limit)
        {
            await RefreshAccessTokenIfExpired();

            var url = "search" + Helpers.CreateUrlQueryString(
                "type", type,
                "q", query,
                "limit", limit.ToString());

            return await apiHttpClient.GetFromJsonAsync<SearchResponse>(url);
        }

        public async Task<Track?> SearchTrack(string query)
        {
            var results = await Search(query, "track", 1);
            return results?.Tracks.Items.FirstOrDefault();
        }

        public async Task<Artist?> SearchArtist(string query)
        {
            var results = await Search(query, "artist", 1);
            return results?.Artists.Items.FirstOrDefault();
        }

        public async Task<AlbumSimple?> SearchAlbum(string query)
        {
            var results = await Search(query, "album", 1);
            return results?.Albums.Items.FirstOrDefault();
        }

        public async Task<bool> TransferPlaybackToPreferredDevice()
        {
            await RefreshAccessTokenIfExpired();

            var url = "me/player";

            var response = await apiHttpClient.PutAsJsonAsync(url, new TransferPlaybackRequest
            {
                DeviceIds = [Cache.DeviceId]
            });

            if (!response.IsSuccessStatusCode)
            {
                var devices = await GetDevices();
                if (devices != null && devices.Count != 0)
                {
                    response = await apiHttpClient.PutAsJsonAsync(url, new TransferPlaybackRequest
                    {
                        DeviceIds = [Cache.DeviceId]
                    });
                }
            }

            return await CheckForSuccessAndLogWarningOnError(response,
                errorMessage: $"Failed to transfer playback to device '{Cache.DeviceId}' due to an error: {0}");
        }

        public async Task<bool> Pause()
        {
            await RefreshAccessTokenIfExpired();

            var url = "me/player/pause" + Helpers.CreateUrlQueryString(
                "device_id", Cache.DeviceId);

            var response = await apiHttpClient.PutAsync(url, null);

            return await CheckForSuccessAndLogWarningOnError(response,
                errorMessage: "Failed to pause playback due to an error: {0}");
        }

        public async Task<bool> Resume()
        {
            await RefreshAccessTokenIfExpired();

            var url = "me/player/play" + Helpers.CreateUrlQueryString(
                "device_id", Cache.DeviceId);

            var response = await apiHttpClient.PutAsync(url, null);

            if (response.StatusCode == HttpStatusCode.NotFound && await TransferPlaybackToPreferredDevice())
            {
                response = await apiHttpClient.PutAsync(url, null);
            }

            return await CheckForSuccessAndLogWarningOnError(response,
                errorMessage: "Failed to resume playback due to an error: {0}");
        }

        public async Task<bool> SkipToNext()
        {
            await RefreshAccessTokenIfExpired();

            var url = "me/player/next" + Helpers.CreateUrlQueryString(
                "device_id", Cache.DeviceId);

            var response = await apiHttpClient.PostAsync(url, null);

            return await CheckForSuccessAndLogWarningOnError(response,
                errorMessage: "Failed to skip to next track due to an error: {0}");
        }

        public async Task<bool> Play(string uri)
        {
            await RefreshAccessTokenIfExpired();

            var url = "me/player/play" + Helpers.CreateUrlQueryString(
                "device_id", Cache.DeviceId);

            var response = await apiHttpClient.PutAsJsonAsync(url, new PlaybackRequest
            {
                Uris = new[] { uri }
            });

            return await CheckForSuccessAndLogWarningOnError(response,
                errorMessage: "Failed to play a track due to an error: {0}");
        }

        public async Task<bool> Play(string uri, string? contextUri)
        {
            if (contextUri is null) return await Play(uri);

            await RefreshAccessTokenIfExpired();

            var url = "me/player/play" + Helpers.CreateUrlQueryString(
                "device_id", Cache.DeviceId);

            var response = await apiHttpClient.PutAsJsonAsync(url, new PlaybackRequest
            {
                ContextUri = contextUri,
                Offset = new()
                {
                    Uri = uri
                }
            });

            return await CheckForSuccessAndLogWarningOnError(response,
                errorMessage: "Failed to play a track due to an error: {0}");
        }

        public async Task<bool> Play(Track track)
        {
            await RefreshAccessTokenIfExpired();

            var url = "me/player/play" + Helpers.CreateUrlQueryString(
                "device_id", Cache.DeviceId);

            var response = await apiHttpClient.PutAsJsonAsync(url, new PlaybackRequest
            {
                ContextUri = track.Album.Uri,
                Offset = new()
                {
                    Uri = track.Uri
                }
            });

            return await CheckForSuccessAndLogWarningOnError(response,
                errorMessage: "Failed to play a track due to an error: {0}");
        }

        public async Task<bool> Play(Artist artist)
        {
            await RefreshAccessTokenIfExpired();

            var url = "me/player/play" + Helpers.CreateUrlQueryString(
                "device_id", Cache.DeviceId);

            var response = await apiHttpClient.PutAsJsonAsync(url, new PlaybackRequest
            {
                ContextUri = artist.Uri
            });

            return await CheckForSuccessAndLogWarningOnError(response,
                errorMessage: "Failed to play a track from artist due to an error: {0}");
        }

        public async Task<bool> Play(IEnumerable<Track> tracks)
        {
            return await Play(tracks.Select(track => track.Uri));
        }

        public async Task<bool> Play(IEnumerable<string> uris)
        {
            await RefreshAccessTokenIfExpired();

            var url = "me/player/play" + Helpers.CreateUrlQueryString(
                "device_id", Cache.DeviceId);

            var response = await apiHttpClient.PutAsJsonAsync(url, new PlaybackRequest
            {
                Uris = uris.ToArray()
            });

            return await CheckForSuccessAndLogWarningOnError(response,
                errorMessage: "Failed to play a track due to an error: {0}");
        }

        public async Task<bool> SetVolume(int volumePercent)
        {
            await RefreshAccessTokenIfExpired();

            var url = "me/player/volume" + Helpers.CreateUrlQueryString(
                "device_id", Cache.DeviceId,
                "volume_percent", volumePercent.ToString());

            var response = await apiHttpClient.PutAsync(url, null);

            return response.IsSuccessStatusCode;
        }


        private async Task<bool> CheckForSuccessAndLogWarningOnError(HttpResponseMessage response, string errorMessage)
        {
            if (response.IsSuccessStatusCode) return true;

            Error? error;

            try
            {
                error = await response.Content.ReadFromJsonAsync<Error>();
            }
            catch
            {
                error = null;
            }

            if (string.IsNullOrEmpty(error?.Message))
            {
                logger?.LogWarning(errorMessage, response.StatusCode);
            }
            else
            {
                logger?.LogWarning(errorMessage, error.Message);
            }

            return false;
        }

        private static class Helpers
        {
            public static string CreateUrlQueryString(params string?[] parameters)
            {
                var sb = new StringBuilder();
                var separator = '?';

                for (int i = 0; i < parameters.Length; i += 2)
                {
                    var name = parameters[i];
                    var value = parameters[i + 1];

                    if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value)) continue;

                    sb.Append(separator);
                    sb.Append(Uri.EscapeDataString(name));
                    sb.Append('=');
                    sb.Append(Uri.EscapeDataString(value));

                    separator = '&';
                }

                return sb.ToString();
            }
        }
    }

    public record Cache
    {
        [JsonPropertyName("device_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DeviceId { get; set; }

        [JsonPropertyName("preferred_device_name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PreferredDeviceName { get; set; }

        [JsonPropertyName("refresh_token")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RefreshToken { get; set; }
    }

    public record Credentials
    {
        [JsonPropertyName("client_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ClientId { get; set; }

        [JsonPropertyName("client_secret")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ClientSecret { get; set; }
    }
}
