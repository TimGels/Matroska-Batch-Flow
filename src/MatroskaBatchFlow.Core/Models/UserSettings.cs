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

    [JsonPropertyName("UI")]
    public UISettings UI { get; set; } = new UISettings();

    public sealed class MkvPropeditSettings
    {
        [JsonPropertyName("CustomPath")]
        public string? CustomPath { get; set; }

        [JsonPropertyName("IsCustomPathEnabled")]
        public bool IsCustomPathEnabled { get; set; }
    }

    public sealed class UISettings
    {
        /// <summary>
        /// Gets or sets whether to show detailed file count text ("X/Y files") alongside track availability indicators.
        /// When false, only colored dots are shown. When true, dots are shown with text.
        /// </summary>
        [JsonPropertyName("ShowTrackAvailabilityText")]
        public bool ShowTrackAvailabilityText { get; set; } = true;

        /// <summary>
        /// Gets or sets the application theme preference.
        /// Valid values: "System", "Light", or "Dark".
        /// </summary>
        [JsonPropertyName("Theme")]
        public string Theme { get; set; } = "System";
    }
}
