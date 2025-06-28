using System.Text.Json.Serialization;

namespace MatroskaBatchFlow.Core.Models;

public record MatroskaLanguageOption
{
    [JsonPropertyName("name")]
    public string Name { get; }

    /// <summary>
    /// Gets the ISO 639-1 code for the language.
    /// </summary>
    /// <remarks>This property may be null if the 
    /// language does not have an assigned ISO 
    /// 639-1 code.</remarks>
    [JsonPropertyName("iso639_1")]
    public string? Iso639_1 { get; }

    [JsonPropertyName("iso639_2_b")]
    public string Iso639_2_b { get; }

    [JsonPropertyName("iso639_2_t")]
    public string Iso639_2_t { get; }

    [JsonPropertyName("iso639_3")]
    public string Iso639_3 { get; }

    public string FormattedName => $"{Name} ({Iso639_2_b})";

    public string Code => (!string.IsNullOrEmpty(Iso639_1) ? Iso639_1 : Iso639_2_b) ?? "und";

    public MatroskaLanguageOption(
        string name,
        string? iso639_1,
        string iso639_2_b,
        string iso639_2_t,
        string iso639_3
    )
    {
        Name = name;
        Iso639_1 = iso639_1;
        Iso639_2_b = iso639_2_b;
        Iso639_2_t = iso639_2_t;
        Iso639_3 = iso639_3;
    }

    /// <summary>
    /// Identifies linguistic content whose language is not determined.
    /// </summary>
    /// <remarks>This option is used when the language cannot be determined. The ISO 639 codes 
    /// for this option are set to "und" (undetermined), except for the ISO 639-1 code, which is
    /// <see langword="null"/>.</remarks>
    public static readonly MatroskaLanguageOption Undetermined = new(
        name: "Undetermined",
        iso639_1: null,
        iso639_2_b: "und",
        iso639_2_t: "und",
        iso639_3: "und"
    );

    public override string ToString() => Name;
}
