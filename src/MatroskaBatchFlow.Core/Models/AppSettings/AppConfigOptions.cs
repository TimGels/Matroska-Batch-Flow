using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MatroskaBatchFlow.Core.Models.AppSettings;

public class AppConfigOptions
{
    [Required]
    [JsonPropertyName("Environment")]
    public string Environment { get; init; }

    [Required]
    [JsonPropertyName("MkvPropeditPath")]
    public string MkvPropeditPath { get; init; }

    [Required]
    [JsonPropertyName("UserSettingsPath")]
    public string UserSettingsPath { get; init; }
}
