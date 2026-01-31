using MatroskaBatchFlow.Uno.Models;
using Serilog.Events;

namespace MatroskaBatchFlow.Uno.UnitTests.Models;

/// <summary>
/// Contains unit tests for the <see cref="LogEntry"/> record.
/// </summary>
public class LogEntryTests
{
    [Fact]
    public void FormattedMessage_WithoutException_ReturnsCorrectFormat()
    {
        // Arrange
        var timestamp = new DateTime(2026, 1, 31, 14, 30, 45, 123);
        var entry = new LogEntry(timestamp, LogEventLevel.Information, "Test message", null);

        // Act
        var result = entry.FormattedMessage;

        // Assert
        Assert.Equal("[14:30:45.123] [INF] Test message", result);
    }

    [Fact]
    public void FormattedMessage_WithException_IncludesExceptionText()
    {
        // Arrange
        var timestamp = new DateTime(2026, 1, 31, 14, 30, 45, 123);
        var exceptionText = "System.Exception: Something went wrong";
        var entry = new LogEntry(timestamp, LogEventLevel.Error, "Error occurred", exceptionText);

        // Act
        var result = entry.FormattedMessage;

        // Assert
        Assert.Equal($"[14:30:45.123] [ERR] Error occurred\n{exceptionText}", result);
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose, "VRB")]
    [InlineData(LogEventLevel.Debug, "DBG")]
    [InlineData(LogEventLevel.Information, "INF")]
    [InlineData(LogEventLevel.Warning, "WRN")]
    [InlineData(LogEventLevel.Error, "ERR")]
    [InlineData(LogEventLevel.Fatal, "FTL")]
    public void FormattedMessage_WithDifferentLogLevels_UsesCorrectAbbreviation(LogEventLevel level, string expectedAbbreviation)
    {
        // Arrange
        var timestamp = new DateTime(2026, 1, 31, 12, 0, 0, 0);
        var entry = new LogEntry(timestamp, level, "Test", null);

        // Act
        var result = entry.FormattedMessage;

        // Assert
        Assert.Contains($"[{expectedAbbreviation}]", result);
    }

    [Fact]
    public void LogEntry_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var timestamp = new DateTime(2026, 1, 31, 14, 30, 45, 123);
        var entry1 = new LogEntry(timestamp, LogEventLevel.Information, "Test message", null);
        var entry2 = new LogEntry(timestamp, LogEventLevel.Information, "Test message", null);
        var entry3 = new LogEntry(timestamp, LogEventLevel.Warning, "Test message", null);

        // Act & Assert
        Assert.Equal(entry1, entry2);
        Assert.NotEqual(entry1, entry3);
    }
}
