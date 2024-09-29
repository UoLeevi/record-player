using System.Text.Json.Serialization;

namespace Spotify
{
    public record ErrorResponse
    {
        [JsonPropertyName("error")]
        public Error Error { get; set; } = new();
    }
}
