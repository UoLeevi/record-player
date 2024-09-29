using System.Text.Json.Serialization;

namespace Spotify
{
    public record ExternalUrls
    {
        [JsonPropertyName("spotify")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Spotify { get; set; }
    }
}
