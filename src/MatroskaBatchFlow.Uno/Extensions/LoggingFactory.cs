using MatroskaBatchFlow.Core.Models.AppSettings;
using MatroskaBatchFlow.Uno.Services;
using Serilog;
using Serilog.Core;

namespace MatroskaBatchFlow.Uno.Extensions;

/// <summary>
/// Factory for creating the application logger before DI is available.
/// </summary>
public static class LoggingFactory
{
    /// <summary>
    /// Configures and creates the Serilog logger with settings from configuration.
    /// </summary>
    /// <param name="levelSwitch">The logging level switch for dynamic log level control.</param>
    /// <param name="loggingViewService">The logging view service to receive log events.</param>
    /// <param name="loggingOptions">The logging options from configuration.</param>
    /// <param name="logPath">The file path for log output.</param>
    /// <returns>The configured logger.</returns>
    public static Logger CreateAppLogger(LoggingLevelSwitch levelSwitch, LoggingViewService loggingViewService, LoggingOptions loggingOptions, string logPath)
    {
        ArgumentNullException.ThrowIfNull(levelSwitch);
        ArgumentNullException.ThrowIfNull(loggingViewService);
        ArgumentNullException.ThrowIfNull(loggingOptions);
        ArgumentException.ThrowIfNullOrWhiteSpace(logPath);

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .WriteTo.Debug()
            .WriteTo.File(
                logPath,
                rollingInterval: Enum.TryParse<RollingInterval>(loggingOptions.RollingInterval, out var rollingInterval)
                    ? rollingInterval
                    : RollingInterval.Day,
                retainedFileCountLimit: loggingOptions.RetainedFileCountLimit,
                fileSizeLimitBytes: loggingOptions.FileSizeLimitBytes,
                rollOnFileSizeLimit: loggingOptions.RollOnFileSizeLimit)
            .Enrich.FromLogContext()
            .WriteTo.Sink(loggingViewService.Sink);

        return loggerConfig.CreateLogger();
    }
}
