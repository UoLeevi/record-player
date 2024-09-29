using System.Text.Json.Serialization;

namespace Spotify
{
    public record Restrictions
    {
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = "";
    }
}
