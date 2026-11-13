using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.UnitTests.Builders;

namespace MatroskaBatchFlow.Core.UnitTests.Models;

/// <summary>
/// Contains unit tests for the progress tracking of <see cref="BatchExecutionReport"/>.
/// </summary>
public class BatchExecutionReportTests
{
    [Fact]
    public void ProgressProperties_ReflectCompletedAndRunningFiles()
    {
        // Arrange
        var batch = new BatchExecutionReport();
        var first = CreateReport("first.mkv");
        var second = CreateReport("second.mkv");
        var third = CreateReport("third.mkv");
        var fourth = CreateReport("fourth.mkv");
        batch.TryAddFileReport(first);
        batch.TryAddFileReport(second);
        batch.TryAddFileReport(third);
        batch.TryAddFileReport(fourth);

        // Act
        first.Status = ProcessingStatus.Succeeded;
        second.Status = ProcessingStatus.Failed;
        third.Status = ProcessingStatus.Running;
        third.Progress = 50;

        // Assert
        Assert.Equal(2, batch.Completed);
        Assert.Same(third, batch.CurrentFile);
        Assert.Equal(2.5 / 4, batch.OverallProgress, precision: 6);
    }

    [Fact]
    public void OverallProgress_RaisesPropertyChanged_WhenCurrentFileProgressChanges()
    {
        // Arrange
        var batch = new BatchExecutionReport();
        var report = CreateReport("file.mkv");
        batch.TryAddFileReport(report);
        report.Status = ProcessingStatus.Running;
        var changed = new List<string?>();
        batch.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        // Act
        report.Progress = 25;

        // Assert
        Assert.Contains(nameof(BatchExecutionReport.OverallProgress), changed);
        Assert.Equal(0.25, batch.OverallProgress, precision: 6);
    }

    [Fact]
    public void CanceledFiles_DoNotCountAsProgress()
    {
        // Arrange
        var batch = new BatchExecutionReport();
        var processed = CreateReport("processed.mkv");
        var stopped = CreateReport("stopped.mkv");
        var notStarted = CreateReport("not-started.mkv");
        batch.TryAddFileReport(processed);
        batch.TryAddFileReport(stopped);
        batch.TryAddFileReport(notStarted);
        processed.Status = ProcessingStatus.Succeeded;
        stopped.Status = ProcessingStatus.Running;
        stopped.Progress = 80;

        // Act
        stopped.Status = ProcessingStatus.Canceled;
        notStarted.Status = ProcessingStatus.Canceled;

        // Assert
        Assert.Null(batch.CurrentFile);
        Assert.Equal(1, batch.Completed);
        Assert.Equal(1d / 3, batch.OverallProgress, precision: 6);
    }

    [Fact]
    public void AddFileReports_SkipsDuplicates_AndUpdatesSummaryOnce()
    {
        // Arrange
        var batch = new BatchExecutionReport();
        var existing = CreateReport("existing.mkv");
        batch.TryAddFileReport(existing);
        var first = CreateReport("first.mkv");
        var second = CreateReport("second.mkv");
        var totalNotifications = 0;
        batch.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(BatchExecutionReport.Total))
                totalNotifications++;
        };

        // Act
        var added = batch.AddFileReports([first, existing, second, first]);

        // Assert
        Assert.Equal([first, second], added);
        Assert.Equal(3, batch.Total);
        Assert.Equal(3, batch.Pending);
        Assert.Equal(1, totalNotifications);
    }

    private static FileProcessingReport CreateReport(string path)
    {
        var mediaInfoResult = new MediaInfoResultBuilder()
            .WithCreatingLibrary()
            .AddTrackOfType(TrackType.General)
            .Build();

        return new FileProcessingReport
        {
            SourceFile = new ScannedFileInfo(mediaInfoResult, path),
            Path = path
        };
    }
}
