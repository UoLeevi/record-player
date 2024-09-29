using System.Text.Json.Serialization;

namespace Spotify
{
    public record Copyright
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "C";
    }
}
