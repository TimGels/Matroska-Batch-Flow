using System.Collections.ObjectModel;
using MatroskaBatchFlow.Uno.Models;
using Serilog.Core;

namespace MatroskaBatchFlow.Uno.Contracts.Services;

/// <summary>
/// Service for capturing and exposing log entries for display in the UI.
/// </summary>
public interface ILoggingViewService
{
    /// <summary>
    /// Gets the observable collection of log entries.
    /// </summary>
    ObservableCollection<LogEntry> LogEntries { get; }

    /// <summary>
    /// Gets the Serilog sink that captures log entries.
    /// </summary>
    ILogEventSink Sink { get; }

    /// <summary>
    /// Gets all log entries as a single formatted text string.
    /// </summary>
    /// <returns>A string containing all log entries.</returns>
    string GetAllLogsAsText();

    /// <summary>
    /// Clears all captured log entries.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets a value indicating whether the logging view service is active.
    /// </summary>
    bool IsEnabled { get; }
}
