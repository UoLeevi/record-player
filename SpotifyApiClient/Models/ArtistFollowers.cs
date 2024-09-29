using System.Text.Json.Serialization;

namespace Spotify
{
    public record ArtistFollowers
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }
    }
}
