using System.Text.Json.Serialization;
using MatroskaBatchFlow.Core.Models.AppSettings;

namespace MatroskaBatchFlow.Core.Models;

public sealed class UserSettings
{
    public const int CurrentSchemaVersion = 1;

    [JsonPropertyName("SchemaVersion")]
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    [JsonPropertyName("MkvPropedit")]
    public MkvPropeditSettings MkvPropedit { get; set; } = new MkvPropeditSettings();

    [JsonPropertyName("BatchValidation")]
    public BatchValidationSettings BatchValidation { get; set; } = new BatchValidationSettings();

    public sealed class MkvPropeditSettings
    {
        [JsonPropertyName("CustomPath")]
        public string? CustomPath { get; set; }

        [JsonPropertyName("IsCustomPathEnabled")]
        public bool IsCustomPathEnabled { get; set; }
    }
}
