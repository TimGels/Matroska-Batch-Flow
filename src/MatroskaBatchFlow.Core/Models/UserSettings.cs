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

    [JsonPropertyName("BatchHistory")]
    public BatchHistorySettings BatchHistory { get; set; } = new BatchHistorySettings();

    public sealed class BatchHistorySettings
    {
        /// <summary>
        /// The number of finished batches that is kept by default.
        /// </summary>
        public const int DefaultMaxRuns = 20;

        /// <summary>
        /// The lowest number of finished batches that can be kept, so the last batch can always be reviewed.
        /// </summary>
        public const int MinMaxRuns = 1;

        /// <summary>
        /// The highest number of finished batches that can be kept.
        /// </summary>
        public const int MaxMaxRuns = 100;

        /// <summary>
        /// Gets or sets how many finished batches the Results page keeps. Older batches are removed.
        /// </summary>
        [JsonPropertyName("MaxRuns")]
        public int MaxRuns { get; set; } = DefaultMaxRuns;
    }

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

        /// <summary>
        /// Gets or sets the minimum log level.
        /// Valid values: "Trace", "Debug", "Information", "Warning", "Error", "Critical".
        /// </summary>
        [JsonPropertyName("LogLevel")]
        public string LogLevel { get; set; } = "Information";

        /// <summary>
        /// Gets or sets whether the logging viewer tab is enabled in the navigation menu.
        /// </summary>
        [JsonPropertyName("EnableLoggingView")]
        public bool EnableLoggingView { get; set; } = false;

        /// <summary>
        /// Gets or sets whether the command preview button is shown in the main window.
        /// </summary>
        [JsonPropertyName("ShowMkvpropeditArguments")]
        public bool ShowMkvpropeditArguments { get; set; } = false;
    }
}
