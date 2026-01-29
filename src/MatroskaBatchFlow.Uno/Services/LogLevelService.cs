using Serilog.Core;
using Serilog.Events;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// Service for dynamically controlling the application's logging level at runtime.
/// </summary>
public interface ILogLevelService
{
    /// <summary>
    /// Gets or sets the minimum log level.
    /// </summary>
    LogEventLevel MinimumLevel { get; set; }
}

/// <summary>
/// Implementation of <see cref="ILogLevelService"/> using Serilog's LoggingLevelSwitch.
/// </summary>
public sealed class LogLevelService : ILogLevelService
{
    private readonly LoggingLevelSwitch _levelSwitch;

    public LogLevelService(LoggingLevelSwitch levelSwitch)
    {
        _levelSwitch = levelSwitch ?? throw new ArgumentNullException(nameof(levelSwitch));
    }

    /// <inheritdoc/>
    public LogEventLevel MinimumLevel
    {
        get => _levelSwitch.MinimumLevel;
        set => _levelSwitch.MinimumLevel = value;
    }
}
