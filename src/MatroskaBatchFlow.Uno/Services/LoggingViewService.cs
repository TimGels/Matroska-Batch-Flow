using System.Collections.ObjectModel;
using System.Text;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Models;
using Microsoft.UI.Dispatching;
using Serilog.Core;
using Serilog.Events;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// Active implementation of <see cref="ILoggingViewService"/> that captures log entries
/// and exposes them for display in the UI.
/// </summary>
public sealed class LoggingViewService : ILoggingViewService
{
    /// <summary>
    /// Maximum number of log entries to keep in memory.
    /// Older entries are discarded when this limit is exceeded.
    /// </summary>
    private const int MaxLogEntries = 3000;

    private readonly ObservableCollectionSink _sink;
    private readonly ObservableCollection<LogEntry> _logEntries = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingViewService"/> class.
    /// </summary>
    public LoggingViewService()
    {
        _sink = new ObservableCollectionSink(_logEntries, MaxLogEntries);
    }

    /// <summary>
    /// Sets the dispatcher queue for UI thread synchronization. 
    /// This should be called once the main window is available.
    /// </summary>
    /// <param name="dispatcherQueue">The dispatcher queue to use for UI updates.</param>
    public void SetDispatcherQueue(DispatcherQueue dispatcherQueue)
    {
        _sink.SetDispatcherQueue(dispatcherQueue);
    }

    /// <inheritdoc/>
    public ObservableCollection<LogEntry> LogEntries => _logEntries;

    /// <inheritdoc/>
    public ILogEventSink Sink => _sink;

    /// <inheritdoc/>
    public bool IsEnabled => true;

    /// <inheritdoc/>
    public string GetAllLogsAsText()
    {
        var sb = new StringBuilder();
        foreach (var entry in _logEntries)
        {
            sb.AppendLine(entry.FormattedMessage);
        }
        return sb.ToString();
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _logEntries.Clear();
    }

    /// <summary>
    /// Custom Serilog sink that writes log events to an ObservableCollection.
    /// Buffers events until a DispatcherQueue is available, then dispatches to UI thread.
    /// </summary>
    private sealed class ObservableCollectionSink : ILogEventSink
    {
        private readonly ObservableCollection<LogEntry> _logEntries;
        private readonly int _maxEntries;
        private readonly List<LogEntry> _pendingEntries = [];
        private readonly Lock _lock = new();
        private DispatcherQueue? _dispatcherQueue;

        public ObservableCollectionSink(ObservableCollection<LogEntry> logEntries, int maxEntries)
        {
            _logEntries = logEntries;
            _maxEntries = maxEntries;
        }

        /// <summary>
        /// Sets the dispatcher queue to be used for processing log entries on the UI thread.
        /// </summary>
        /// <param name="dispatcherQueue">The dispatcher queue that will be used to enqueue log entry processing operations. Cannot be null.</param>
        public void SetDispatcherQueue(DispatcherQueue dispatcherQueue)
        {
            List<LogEntry>? pending = null;

            lock (_lock)
            {
                _dispatcherQueue = dispatcherQueue;

                // Capture any pending entries to flush.
                if (_pendingEntries.Count > 0)
                {
                    pending = [.. _pendingEntries];
                    _pendingEntries.Clear();
                }
            }

            if (pending is null)
            {
                return;
            }

            // Flush any pending entries to the UI
            dispatcherQueue.TryEnqueue(() =>
            {
                foreach (var entry in pending)
                {
                    AddEntryWithLimit(entry);
                }
            });
        }

        /// <summary>
        /// Processes the specified log event and adds it to the log entry collection, buffering or dispatching as
        /// appropriate based on dispatcher availability.
        /// </summary>
        /// <param name="logEvent">The log event to process and emit. Cannot be null.</param>
        public void Emit(LogEvent logEvent)
        {
            var entry = new LogEntry(
                logEvent.Timestamp.LocalDateTime,
                logEvent.Level,
                logEvent.RenderMessage(),
                logEvent.Exception?.ToString());

            lock (_lock)
            {
                if (_dispatcherQueue is null)
                {
                    // Buffer until dispatcher is available
                    _pendingEntries.Add(entry);
                }
                else if (_dispatcherQueue.HasThreadAccess)
                {
                    AddEntryWithLimit(entry);
                }
                else
                {
                    _dispatcherQueue.TryEnqueue(() => AddEntryWithLimit(entry));
                }
            }
        }

        private void AddEntryWithLimit(LogEntry entry)
        {
            _logEntries.Add(entry);

            // Remove oldest entries if we exceed the limit
            while (_logEntries.Count > _maxEntries)
            {
                _logEntries.RemoveAt(0);
            }
        }
    }
}
