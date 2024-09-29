using System.Text.Json.Serialization;

namespace Spotify
{
    public record CurrentlyPlayingResponse
    {
        [JsonPropertyName("device")]
        public Device Device { get; set; } = new();

        [JsonPropertyName("repeat_state")]
        public string RepeatState { get; set; } = "off";

        [JsonPropertyName("context")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Context? Context { get; set; }

        //[JsonPropertyName("timestamp")]
        //public int Timestamp { get; set; }

        [JsonPropertyName("progress_ms")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ProgressMs { get; set; }

        [JsonPropertyName("is_playing")]
        public bool IsPlaying { get; set; }

        [JsonPropertyName("item")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Track? Item { get; set; }

        [JsonPropertyName("currently_playing_type")]
        public string CurrentlyPlayingType { get; set; } = "";

        [JsonPropertyName("actions")]
        public Actions Actions { get; set; } = new();
    }
}
