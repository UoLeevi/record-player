using System.Text.Json.Serialization;

namespace Spotify
{
    public record TransferPlaybackRequest
    {
        [JsonPropertyName("device_ids")]
        public required string[] DeviceIds { get; set; } 

        [JsonPropertyName("play")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Play { get; set; }
    }
}
