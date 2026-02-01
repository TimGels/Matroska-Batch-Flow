using Serilog.Events;

namespace MatroskaBatchFlow.Uno.Contracts.Services;

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
