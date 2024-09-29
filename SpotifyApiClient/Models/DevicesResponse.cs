using System.Text.Json.Serialization;

namespace Spotify
{
    public record DevicesResponse
    {
        [JsonPropertyName("devices")]
        public List<Device> Devices { get; set; } = new();
    }
}
