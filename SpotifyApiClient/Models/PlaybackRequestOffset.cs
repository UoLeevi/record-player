using System.Text.Json.Serialization;

namespace Spotify
{
    public record PlaybackRequestOffset
    {
        [JsonPropertyName("uri")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Uri { get; set; }

        [JsonPropertyName("position")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Position { get; set; }
    }
}
