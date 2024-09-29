using System.Text.Json.Serialization;

namespace Spotify
{
    public record ExternalIds
    {
        [JsonPropertyName("isrc")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        // International Standard Recording Code
        public string? Isrc { get; set; }

        [JsonPropertyName("ean")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        // International Article Number
        public string? Ean { get; set; }

        [JsonPropertyName("upc")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        // Universal Product Code
        public string? Upc { get; set; }
    }
}
