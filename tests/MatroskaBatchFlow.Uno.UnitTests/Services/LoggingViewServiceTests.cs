using MatroskaBatchFlow.Uno.Models;
using MatroskaBatchFlow.Uno.Services;
using Serilog.Events;

namespace MatroskaBatchFlow.Uno.UnitTests.Services;

/// <summary>
/// Contains unit tests for the <see cref="LoggingViewService"/> class.
/// </summary>
public class LoggingViewServiceTests
{
    [Fact]
    public void LogEntries_WhenEmpty_ReturnsEmptyCollection()
    {
        // Arrange
        var service = new LoggingViewService();

        // Act
        var entries = service.LogEntries;

        // Assert
        Assert.NotNull(entries);
        Assert.Empty(entries);
    }

    [Fact]
    public void IsEnabled_ReturnsTrue()
    {
        // Arrange
        var service = new LoggingViewService();

        // Act & Assert
        Assert.True(service.IsEnabled);
    }

    [Fact]
    public void Sink_ReturnsNonNullSink()
    {
        // Arrange
        var service = new LoggingViewService();

        // Act
        var sink = service.Sink;

        // Assert
        Assert.NotNull(sink);
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        // Arrange
        var service = new LoggingViewService();
        
        // Manually add entries to the collection for testing
        service.LogEntries.Add(new LogEntry(DateTime.Now, LogEventLevel.Information, "Test 1", null));
        service.LogEntries.Add(new LogEntry(DateTime.Now, LogEventLevel.Warning, "Test 2", null));
        service.LogEntries.Add(new LogEntry(DateTime.Now, LogEventLevel.Error, "Test 3", null));
        
        Assert.Equal(3, service.LogEntries.Count);

        // Act
        service.Clear();

        // Assert
        Assert.Empty(service.LogEntries);
    }

    [Fact]
    public void GetAllLogsAsText_WhenEmpty_ReturnsEmptyString()
    {
        // Arrange
        var service = new LoggingViewService();

        // Act
        var result = service.GetAllLogsAsText();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetAllLogsAsText_WithEntries_ReturnsFormattedText()
    {
        // Arrange
        var service = new LoggingViewService();
        var timestamp1 = new DateTime(2026, 1, 31, 14, 30, 0, 0);
        var timestamp2 = new DateTime(2026, 1, 31, 14, 31, 0, 0);
        
        service.LogEntries.Add(new LogEntry(timestamp1, LogEventLevel.Information, "First message", null));
        service.LogEntries.Add(new LogEntry(timestamp2, LogEventLevel.Warning, "Second message", null));

        // Act
        var result = service.GetAllLogsAsText();

        // Assert
        Assert.Contains("[14:30:00.000] [INF] First message", result);
        Assert.Contains("[14:31:00.000] [WRN] Second message", result);
    }

    [Fact]
    public void GetAllLogsAsText_WithMultipleEntries_SeparatesWithNewlines()
    {
        // Arrange
        var service = new LoggingViewService();
        service.LogEntries.Add(new LogEntry(DateTime.Now, LogEventLevel.Information, "Line 1", null));
        service.LogEntries.Add(new LogEntry(DateTime.Now, LogEventLevel.Information, "Line 2", null));

        // Act
        var result = service.GetAllLogsAsText();
        var lines = result.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        // Assert
        Assert.Equal(2, lines.Length);
    }

    [Fact]
    public void Sink_EmitLogEvent_BuffersWhenNoDispatcher()
    {
        // Arrange
        var service = new LoggingViewService();
        
        // Use Serilog's Log.Logger to create a proper LogEvent
        var logger = new Serilog.LoggerConfiguration()
            .WriteTo.Sink(service.Sink)
            .CreateLogger();

        // Act - emit without dispatcher set (log entries should be buffered)
        logger.Information("Test message");

        // Assert - entries should be buffered (not in LogEntries yet since no dispatcher)
        Assert.Empty(service.LogEntries);
    }

    [Fact]
    public void LogEntries_IsObservableCollection()
    {
        // Arrange
        var service = new LoggingViewService();
        var collectionChangedRaised = false;
        
        service.LogEntries.CollectionChanged += (s, e) => collectionChangedRaised = true;

        // Act
        service.LogEntries.Add(new LogEntry(DateTime.Now, LogEventLevel.Information, "Test", null));

        // Assert
        Assert.True(collectionChangedRaised);
    }
}
