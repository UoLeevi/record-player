using System.Text.Json.Serialization;

namespace Spotify
{
    public record Actions
    {
        [JsonPropertyName("interupting_playback")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? InteruptingPlayback { get; set; }

        [JsonPropertyName("pausing")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Pausing { get; set; }

        [JsonPropertyName("resuming")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Resuming { get; set; }

        [JsonPropertyName("seeking")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Seeking { get; set; }

        [JsonPropertyName("skipping_next")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? SkippingNext { get; set; }

        [JsonPropertyName("skipping_prev")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? SkippingPrev { get; set; }

        [JsonPropertyName("toggling_repeat_context")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? TogglingRepeatContext { get; set; }

        [JsonPropertyName("toggling_shuffle")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? TogglingShuffle { get; set; }

        [JsonPropertyName("toggling_repeat_track")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? TogglingRepeatTrack { get; set; }

        [JsonPropertyName("transferring_playback")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? TransferringPlayback { get; set; }
    }
}
