using System.Text.Json.Serialization;

namespace Spotify
{
    // https://developer.spotify.com/documentation/web-api/reference/get-track
    public record Track
    {
        [JsonPropertyName("album")]
        public Album Album { get; set; } = new();

        [JsonPropertyName("artists")]
        public List<Artist> Artists { get; set; } = new();

        [JsonPropertyName("available_markets")]
        public List<string> AvailableMarkets { get; set; } = new();

        [JsonPropertyName("disc_number")]
        public int DiscNumber { get; set; }

        [JsonPropertyName("duration_ms")]
        public int DurationMs { get; set; }

        [JsonPropertyName("explicit")]
        public bool Explicit { get; set; }

        [JsonPropertyName("external_ids")]
        public ExternalIds ExternalIds { get; set; } = new();

        [JsonPropertyName("external_urls")]
        public ExternalUrls ExternalUrls { get; set; } = new();

        [JsonPropertyName("href")]
        public string Href { get; set; } = "";

        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("is_playable")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsPlayable { get; set; }

        [JsonPropertyName("restrictions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Restrictions? Restrictions { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("popularity")]
        public int Popularity { get; set; }

        [JsonPropertyName("preview_url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PreviewUrl { get; set; }

        [JsonPropertyName("track_number")]
        public int TrackNumber { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "track";

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";

        [JsonPropertyName("is_local")]
        public bool IsLocal { get; set; }
    }
}
