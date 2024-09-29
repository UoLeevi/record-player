using System.Text.Json.Serialization;

namespace Spotify
{
    public record PlaybackState
    {
        [JsonPropertyName("device")]
        public Device Device { get; set; } = new();

        [JsonPropertyName("is_playing")]
        public bool IsPlaying { get; set; }

        [JsonPropertyName("item")]
        public Track Item { get; set; }
    }
}
