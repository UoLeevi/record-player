using System.Text.Json.Serialization;

namespace Spotify
{
    public record Context
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("href")]
        public string Href { get; set; } = "";

        [JsonPropertyName("external_urls")]
        public ExternalUrls ExternalUrls { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";
    }
}
