using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MatroskaBatchFlow.Core.Models.AppSettings;

public class AppConfigOptions
{
    [Required]
    [JsonPropertyName("Environment")]
    public string Environment { get; set; } = "Production";

    [Required]
    [JsonPropertyName("MkvPropeditPath")]
    public string MkvPropeditPath { get; set; } = "mkvpropedit";

    [Required]
    [JsonPropertyName("UserSettingsPath")]
    public string UserSettingsPath { get; set; } = "UserSettings.json";
}
