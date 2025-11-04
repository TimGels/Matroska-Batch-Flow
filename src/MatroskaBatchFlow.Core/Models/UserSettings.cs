using System.Text.Json.Serialization;

namespace MatroskaBatchFlow.Core.Models;

public sealed class UserSettings
{
    [JsonRequired]
    [JsonPropertyName("Version")]
    public int SchemaVersion { get; } = 1;

    [JsonRequired]
    [JsonPropertyName("MkvPropedit")]
    public MkvPropeditSettings MkvPropedit { get; set; } = new MkvPropeditSettings();

    public sealed class MkvPropeditSettings
    {
        [JsonPropertyName("CustomPath")]
        public string? CustomPath { get; set; }

        [JsonPropertyName("IsCustomPathEnabled")]
        public bool IsCustomPathEnabled { get; set; }
    }
}
