using System.Text.Json.Serialization;

namespace Spotify
{
    public record SearchResponse
    {
        [JsonPropertyName("tracks")]
        public ListPage<Track> Tracks { get; set; } = new();

        [JsonPropertyName("artists")]
        public ListPage<Artist> Artists { get; set; } = new();

        [JsonPropertyName("albums")]
        public ListPage<AlbumSimple> Albums { get; set; } = new();
    }
}
