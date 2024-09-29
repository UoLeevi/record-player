using System.Text.Json.Serialization;

namespace Spotify
{
    public record ListPage<T>
    {
        [JsonPropertyName("href")]
        public string Href { get; set; } = "";

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("next")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Next { get; set; } = "";

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("previous")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Previous { get; set; } = "";

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("items")]
        public List<T> Items { get; set; } = new();
    }
}
