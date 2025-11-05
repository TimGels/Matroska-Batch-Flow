using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MatroskaBatchFlow.Core.Models.AppSettings;

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
