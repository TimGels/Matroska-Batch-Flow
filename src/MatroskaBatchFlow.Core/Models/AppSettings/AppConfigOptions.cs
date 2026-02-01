using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using MatroskaBatchFlow.Core.Attributes;

namespace MatroskaBatchFlow.Core.Models.AppSettings;

[ValidatedOptions]
public class AppConfigOptions
{
    [Required]
    [JsonPropertyName("Environment")]
    public required string Environment { get; init; }

    [Required]
    [JsonPropertyName("MkvPropeditPath")]
    public required string MkvPropeditPath { get; init; }

    [Required]
    [JsonPropertyName("UserSettingsPath")]
    public required string UserSettingsPath { get; init; }
}
