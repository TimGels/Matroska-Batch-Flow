using MatroskaBatchFlow.Core.Attributes;

namespace MatroskaBatchFlow.Core.Models.AppSettings;

[ValidatedOptions]
public class LanguageOptions
{
    public string FilePath { get; set; } = string.Empty;
}
