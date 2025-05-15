using System.Text.Json.Serialization;

namespace MatroskaBatchFlow.Core.Models
{
    public record MatroskaLanguageOption
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("iso639_1")]
        public string Iso639_1 { get; set; }

        [JsonPropertyName("iso639_2_b")]
        public string Iso639_2_b { get; set; }

        [JsonPropertyName("iso639_2_t")]
        public string Iso639_2_t { get; set; }

        [JsonPropertyName("iso639_3")]
        public string Iso639_3 { get; set; }

        public string FormattedName => $"{Name} ({Iso639_2_b})";

        public string Code => !string.IsNullOrEmpty(Iso639_1) ? Iso639_1 : Iso639_2_b;

        public MatroskaLanguageOption(
            string name,
            string iso639_1,
            string iso639_2_b,
            string iso639_2_t,
            string iso639_3)
        {
            Name = name;
            Iso639_1 = iso639_1;
            Iso639_2_b = iso639_2_b;
            Iso639_2_t = iso639_2_t;
            Iso639_3 = iso639_3;
        }

        public override string ToString() => Name;
    }
}
