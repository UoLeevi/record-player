using System.Text.Json.Serialization;

namespace Spotify
{
    public record Album
    {
        [JsonPropertyName("album_type")]
        // Allowed values: "album", "single", "compilation"
        public string AlbumType { get; set; } = "album";

        [JsonPropertyName("total_tracks")]
        public int TotalTracks { get; set; }

        [JsonPropertyName("available_markets")]
        public List<string> AvailableMarkets { get; set; } = new();

        [JsonPropertyName("external_urls")]
        public ExternalUrls ExternalUrls { get; set; } = new();

        [JsonPropertyName("href")]
        public string Href { get; set; } = "";

        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("images")]
        public List<Image> Images { get; set; } = new();

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("release_date")]
        public string ReleaseDate { get; set; } = "";

        [JsonPropertyName("release_precision")]
        // Allowed values: "year", "month", "day"
        public string ReleasePrecision { get; set; } = "";

        [JsonPropertyName("restrictions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Restrictions? Restrictions { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "album";

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";

        [JsonPropertyName("artists")]
        public List<ArtistSimple> Artists { get; set; } = new();

        [JsonPropertyName("tracks")]
        public ListPage<Track> Tracks { get; set; } = new();

        [JsonPropertyName("copyrights")]
        public List<Copyright> Copyrights { get; set; } = new();

        [JsonPropertyName("external_ids")]
        public ExternalIds ExternalIds { get; set; } = new();

        [JsonPropertyName("genres")]
        public List<string> Genres { get; set; } = new();

        [JsonPropertyName("label")]
        public string Label { get; set; } = "";

        [JsonPropertyName("popularity")]
        public int Popularity { get; set; }
    }
}
