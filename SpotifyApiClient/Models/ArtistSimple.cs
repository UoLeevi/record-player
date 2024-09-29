using System.Text.Json.Serialization;

namespace Spotify
{
    // https://developer.spotify.com/documentation/web-api/reference/get-an-artist
    public record ArtistSimple
    {
        [JsonPropertyName("external_urls")]
        public ExternalUrls ExternalUrls { get; set; } = new();

        [JsonPropertyName("href")]
        public string Href { get; set; } = "";

        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "artist";

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";
    }
}
