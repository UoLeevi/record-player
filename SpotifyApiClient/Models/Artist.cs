using System.Text.Json.Serialization;

namespace Spotify
{
    // https://developer.spotify.com/documentation/web-api/reference/get-an-artist
    public record Artist
    {
        [JsonPropertyName("external_urls")]
        public ExternalUrls ExternalUrls { get; set; } = new();

        [JsonPropertyName("followers")]
        public ArtistFollowers Followers { get; set; } = new();

        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = new();

        [JsonPropertyName("href")]
        public string Href { get; set; } = "";

        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("images")]
        public List<Image> Images { get; set; } = new();

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("popularity")]
        public int Popularity { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "artist";

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";
    }
}
