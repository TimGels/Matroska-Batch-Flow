using Serilog.Events;

namespace MatroskaBatchFlow.Uno.Models;

/// <summary>
/// Represents a single log entry captured from the logging system.
/// </summary>
/// <param name="Timestamp">The date and time when the log entry was created.</param>
/// <param name="Level">The severity level of the log entry.</param>
/// <param name="Message">The log message text.</param>
/// <param name="Exception">The exception details if one was logged, otherwise null.</param>
public sealed record LogEntry(
    DateTime Timestamp,
    LogEventLevel Level,
    string Message,
    string? Exception)
{
    /// <summary>
    /// Gets a formatted string representation of the log entry suitable for display.
    /// </summary>
    public string FormattedMessage => Exception is null
        ? $"[{Timestamp:HH:mm:ss.fff}] [{GetLevelAbbreviation(Level)}] {Message}"
        : $"[{Timestamp:HH:mm:ss.fff}] [{GetLevelAbbreviation(Level)}] {Message}\n{Exception}";

    private static string GetLevelAbbreviation(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => "VRB",
        LogEventLevel.Debug => "DBG",
        LogEventLevel.Information => "INF",
        LogEventLevel.Warning => "WRN",
        LogEventLevel.Error => "ERR",
        LogEventLevel.Fatal => "FTL",
        _ => "???"
    };
}
