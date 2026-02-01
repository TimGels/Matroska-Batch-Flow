using System.ComponentModel.DataAnnotations;
using MatroskaBatchFlow.Core.Attributes;

namespace MatroskaBatchFlow.Core.Models.AppSettings;

/// <summary>
/// Configuration options for application logging.
/// </summary>
[ValidatedOptions]
public sealed class LoggingOptions
{
    /// <summary>
    /// Gets or sets the minimum log level.
    /// Valid values: "Verbose", "Debug", "Information", "Warning", "Error", "Fatal".
    /// Leave empty or null to allow user control via Settings UI.
    /// When set, this will override user preferences.
    /// </summary>
    [RegularExpression("^(Verbose|Debug|Information|Warning|Error|Fatal)?$", 
        ErrorMessage = "MinimumLevel must be one of: Verbose, Debug, Information, Warning, Error, Fatal, or empty.")]
    public string? MinimumLevel { get; set; }

    /// <summary>
    /// Gets or sets the rolling interval for log files.
    /// Valid values: "Infinite", "Year", "Month", "Day", "Hour", "Minute".
    /// </summary>
    [RegularExpression("^(Infinite|Year|Month|Day|Hour|Minute)$", 
        ErrorMessage = "RollingInterval must be one of: Infinite, Year, Month, Day, Hour, Minute.")]
    public string RollingInterval { get; set; } = "Day";

    /// <summary>
    /// Gets or sets the number of log files to retain.
    /// </summary>
    [Range(1, 365)]
    public int RetainedFileCountLimit { get; set; } = 31;

    /// <summary>
    /// Gets or sets the maximum size of a log file in bytes before rolling.
    /// </summary>
    [Range(1_000_000, 100_000_000)]
    public long FileSizeLimitBytes { get; set; } = 10_000_000;

    /// <summary>
    /// Gets or sets whether to roll log files when the size limit is reached.
    /// </summary>
    public bool RollOnFileSizeLimit { get; set; } = true;
}
