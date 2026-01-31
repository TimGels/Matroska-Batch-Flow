using System.Collections.ObjectModel;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// ViewModel for the log viewer page, providing access to captured log entries
/// and commands for interacting with the log display.
/// </summary>
public partial class LogViewerViewModel : ObservableObject
{
    private readonly ILoggingViewService _loggingViewService;

    /// <summary>
    /// Gets the collection of log entries captured since application start.
    /// </summary>
    public ObservableCollection<LogEntry> LogEntries => _loggingViewService.LogEntries;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogViewerViewModel"/> class.
    /// </summary>
    /// <param name="loggingViewService">The logging view service providing access to log entries.</param>
    public LogViewerViewModel(ILoggingViewService loggingViewService)
    {
        _loggingViewService = loggingViewService;
    }

    /// <summary>
    /// Clears all log entries from the display.
    /// </summary>
    [RelayCommand]
    private void Clear()
    {
        _loggingViewService.Clear();
    }
}
