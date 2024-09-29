using System.Text.Json.Serialization;

namespace Spotify
{
    public record PlaybackRequest
    {
        [JsonPropertyName("context_uri")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ContextUri { get; set; }

        [JsonPropertyName("uris")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string[]? Uris { get; set; }

        [JsonPropertyName("offset")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public PlaybackRequestOffset? Offset { get; set; }

        [JsonPropertyName("position_ms")]
        public int PositionMs { get; set; }
    }
}
