using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Uno.Contracts.Services;
using Serilog.Core;
using Serilog.Events;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// Implementation of <see cref="ILogLevelService"/> using Serilog's LoggingLevelSwitch.
/// </summary>
/// <remarks>
/// Automatically applies the configured log level on construction.
/// Priority: appsettings.json > UserSettings.json > default (Information).
/// </remarks>
public sealed class LogLevelService : ILogLevelService
{
    private readonly LoggingLevelSwitch _levelSwitch;

    public LogLevelService(LoggingLevelSwitch levelSwitch, IOptions<LoggingOptions> loggingOptions, IWritableSettings<UserSettings> userSettings)
    {
        ArgumentNullException.ThrowIfNull(levelSwitch);
        ArgumentNullException.ThrowIfNull(loggingOptions);
        ArgumentNullException.ThrowIfNull(userSettings);

        _levelSwitch = levelSwitch;
        ApplyConfiguredLogLevel(loggingOptions.Value, userSettings.Value);
    }

    /// <inheritdoc/>
    public LogEventLevel MinimumLevel
    {
        get => _levelSwitch.MinimumLevel;
        set => _levelSwitch.MinimumLevel = value;
    }

    /// <summary>
    /// Sets the minimum log level for the logger based on the provided configuration options and user settings.
    /// </summary>
    /// <param name="loggingOptions">The logging configuration options to use when determining the minimum log level. If a minimum level is
    /// specified, it takes precedence over user settings.</param>
    /// <param name="userSettings">The user-specific settings that may define a log level if not specified in the logging options.</param>
    private void ApplyConfiguredLogLevel(LoggingOptions loggingOptions, UserSettings userSettings)
    {
        // Priority: appsettings.json > UserSettings.json > default (Information)
        var effectiveLogLevel = !string.IsNullOrWhiteSpace(loggingOptions.MinimumLevel)
            ? loggingOptions.MinimumLevel
            : userSettings.UI.LogLevel;

        if (!string.IsNullOrWhiteSpace(effectiveLogLevel) &&
            Enum.TryParse<LogEventLevel>(effectiveLogLevel, ignoreCase: true, out var configuredLevel))
        {
            _levelSwitch.MinimumLevel = configuredLevel;
        }
    }
}
