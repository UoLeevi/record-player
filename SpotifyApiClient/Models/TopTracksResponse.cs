using System.Text.Json.Serialization;

namespace Spotify
{
    public record TopTracksResponse
    {

        [JsonPropertyName("tracks")]
        public List<Track> Tracks { get; set; } = new();
    }
}
