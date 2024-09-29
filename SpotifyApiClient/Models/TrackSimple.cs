using System.Text.Json.Serialization;

namespace Spotify
{
    public record TrackSimple
    {
        [JsonPropertyName("artists")]
        public List<ArtistSimple> Artists { get; set; } = new();

        [JsonPropertyName("available_markets")]
        public List<string> AvailableMarkets { get; set; } = new();

        [JsonPropertyName("disc_number")]
        public int DiscNumber { get; set; }

        [JsonPropertyName("duration_ms")]
        public int DurationMs { get; set; }

        [JsonPropertyName("explicit")]
        public bool Explicit { get; set; }

        [JsonPropertyName("external_urls")]
        public ExternalUrls ExternalUrls { get; set; } = new();

        [JsonPropertyName("href")]
        public string Href { get; set; } = "";

        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("is_playable")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? IsPlayable { get; set; }

        [JsonPropertyName("restrictions")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Restrictions? Restrictions { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("preview_url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PreviewUrl { get; set; }

        [JsonPropertyName("track_number")]
        public int TrackNumber { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "track";

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";

        [JsonPropertyName("is_local")]
        public bool IsLocal { get; set; }
    }
}
