using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services.FileValidation;
using MatroskaBatchFlow.Core.UnitTests.Builders;

namespace MatroskaBatchFlow.Core.UnitTests.Services.FileValidation;

/// <summary>
/// Contains unit tests for the <see cref="RollingReferenceComparer"/> rolling reference validation logic.
/// </summary>
public class RollingReferenceComparerTests
{
    // -- Edge cases --

    [Fact]
    public void Compare_WhenMatrixIsEmpty_ReturnsNoResults()
    {
        // Arrange
        var matrix = new List<List<bool>>();
        var files = new List<ScannedFileInfo>();

        // Act
        var results = RollingReferenceComparer.Compare(matrix, files, TrackType.Audio, ValidationSeverity.Error, "Default flag").ToList();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Compare_WhenSingleFile_ReturnsNoResults()
    {
        // Arrange
        var matrix = new List<List<bool>> { new() { true, false } };
        var files = new List<ScannedFileInfo> { CreateFileWithAudioTracks("file1.mkv", 2) };

        // Act
        var results = RollingReferenceComparer.Compare(matrix, files, TrackType.Audio, ValidationSeverity.Error, "Default flag").ToList();

        // Assert
        Assert.Empty(results);
    }

    // -- Same track counts (basic reference behavior) --

    [Fact]
    public void Compare_WhenAllValuesMatch_ReturnsNoResults()
    {
        // Arrange
        var matrix = new List<List<bool>>
        {
            new() { true, false },
            new() { true, false },
            new() { true, false }
        };
        var files = new List<ScannedFileInfo>
        {
            CreateFileWithAudioTracks("file1.mkv", 2),
            CreateFileWithAudioTracks("file2.mkv", 2),
            CreateFileWithAudioTracks("file3.mkv", 2)
        };

        // Act
        var results = RollingReferenceComparer.Compare(matrix, files, TrackType.Audio, ValidationSeverity.Error, "Default flag").ToList();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Compare_WhenValuesDiffer_ReturnsMismatchResults()
    {
        // Arrange
        var matrix = new List<List<bool>>
        {
            new() { true, false },  // Reference
            new() { false, false }  // Mismatch at position 0
        };
        var files = new List<ScannedFileInfo>
        {
            CreateFileWithAudioTracks("file1.mkv", 2),
            CreateFileWithAudioTracks("file2.mkv", 2)
        };

        // Act
        var results = RollingReferenceComparer.Compare(matrix, files, TrackType.Audio, ValidationSeverity.Error, "Default flag").ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(ValidationSeverity.Error, results[0].Severity);
        Assert.Contains("position 1", results[0].Message);
        Assert.Contains("Default flag", results[0].Message);
    }

    [Fact]
    public void Compare_ReportsConfiguredSeverity()
    {
        // Arrange
        var matrix = new List<List<string>>
        {
            new() { "eng" },
            new() { "jpn" }  // Mismatch
        };
        var files = new List<ScannedFileInfo>
        {
            CreateFileWithAudioTracks("file1.mkv", 1),
            CreateFileWithAudioTracks("file2.mkv", 1)
        };

        // Act
        var results = RollingReferenceComparer.Compare(matrix, files, TrackType.Audio, ValidationSeverity.Warning, "Language").ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(ValidationSeverity.Warning, results[0].Severity);
    }

    [Fact]
    public void Compare_ReportsCorrectFilePaths()
    {
        // Arrange
        var matrix = new List<List<bool>>
        {
            new() { true },   // Reference
            new() { false }   // Mismatch — should report file2's path
        };
        var files = new List<ScannedFileInfo>
        {
            CreateFileWithAudioTracks("file1.mkv", 1),
            CreateFileWithAudioTracks("file2.mkv", 1)
        };

        // Act
        var results = RollingReferenceComparer.Compare(matrix, files, TrackType.Audio, ValidationSeverity.Error, "Default flag").ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal("file2.mkv", results[0].ValidatedFilePath);
        Assert.Contains("file1.mkv", results[0].Message);
        Assert.Contains("file2.mkv", results[0].Message);
    }

    [Fact]
    public void Compare_WhenMultipleMismatches_ReportsAll()
    {
        // Arrange
        var matrix = new List<List<bool>>
        {
            new() { true, false, true },   // Reference
            new() { false, true, false }   // All three differ
        };
        var files = new List<ScannedFileInfo>
        {
            CreateFileWithAudioTracks("file1.mkv", 3),
            CreateFileWithAudioTracks("file2.mkv", 3)
        };

        // Act
        var results = RollingReferenceComparer.Compare(matrix, files, TrackType.Audio, ValidationSeverity.Error, "Default flag").ToList();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.Message.Contains("position 1"));
        Assert.Contains(results, r => r.Message.Contains("position 2"));
        Assert.Contains(results, r => r.Message.Contains("position 3"));
    }

    [Fact]
    public void Compare_IncludesTrackTypeInMessage()
    {
        // Arrange
        var matrix = new List<List<string>>
        {
            new() { "eng" },
            new() { "jpn" }
        };
        var files = new List<ScannedFileInfo>
        {
            CreateFileWithAudioTracks("file1.mkv", 1),
            CreateFileWithAudioTracks("file2.mkv", 1)
        };

        // Act
        var results = RollingReferenceComparer.Compare(matrix, files, TrackType.Text, ValidationSeverity.Warning, "Language").ToList();

        // Assert
        Assert.Single(results);
        Assert.Contains("Text", results[0].Message);
    }

    // -- Different track counts: rolling reference behavior --

    [Fact]
    public void Compare_WhenFileHasMoreTracks_ValidatesOverlappingPositions()
    {
        // Arrange: File1 has 1 audio track, File2 has 2. Track at position 0 differs.
        var matrix = new List<List<bool>>
        {
            new() { true },         // File1: 1 track
            new() { false, true }   // File2: 2 tracks — position 0 differs from reference
        };
        var files = new List<ScannedFileInfo>
        {
            CreateFileWithAudioTracks("file1.mkv", 1),
            CreateFileWithAudioTracks("file2.mkv", 2)
        };

        // Act
        var results = RollingReferenceComparer.Compare(matrix, files, TrackType.Audio, ValidationSeverity.Error, "Default flag").ToList();

        // Assert: Should detect mismatch at overlapping position 0
        Assert.Single(results);
        Assert.Contains("position 1", results[0].Message);
        Assert.Equal("file2.mkv", results[0].ValidatedFilePath);
    }

    [Fact]
    public void Compare_WhenFileHasMoreTracks_OverlappingMatchesProduceNoResults()
    {
        // Arrange: File1 has 1 audio track, File2 has 2. Overlapping position 0 matches.
        var matrix = new List<List<bool>>
        {
            new() { true },         // File1: 1 track
            new() { true, false }   // File2: 2 tracks — position 0 matches reference
        };
        var files = new List<ScannedFileInfo>
        {
            CreateFileWithAudioTracks("file1.mkv", 1),
            CreateFileWithAudioTracks("file2.mkv", 2)
        };

        // Act
        var results = RollingReferenceComparer.Compare(matrix, files, TrackType.Audio, ValidationSeverity.Error, "Default flag").ToList();

        // Assert: Overlapping track matches, no results
        Assert.Empty(results);
    }

    [Fact]
    public void Compare_WhenReferenceHasFewerTracks_UsesRollingReferenceForExtraTracks()
    {
        // Arrange:
        // File1: [track0]                 <- Reference for position 0
        // File2: [track0, track1]         <- Reference for position 1 (File1 doesn't have it)
        // File3: [track0, track1, track2] <- Reference for position 2
        // File2.track1 vs File3.track1: mismatch
        var matrix = new List<List<bool>>
        {
            new() { true },                // File1: 1 track
            new() { true, false },         // File2: 2 tracks — becomes reference for position 1
            new() { true, true, false }    // File3: 3 tracks — position 1 differs from File2's reference
        };
        var files = new List<ScannedFileInfo>
        {
            CreateFileWithAudioTracks("file1.mkv", 1),
            CreateFileWithAudioTracks("file2.mkv", 2),
            CreateFileWithAudioTracks("file3.mkv", 3)
        };

        // Act
        var results = RollingReferenceComparer.Compare(matrix, files, TrackType.Audio, ValidationSeverity.Error, "Default flag").ToList();

        // Assert: Mismatch at position 1 (File2 is reference, File3 differs)
        Assert.Single(results);
        Assert.Contains("position 2", results[0].Message);
        Assert.Equal("file3.mkv", results[0].ValidatedFilePath);
        Assert.Contains("file2.mkv", results[0].Message);  // Rolling reference file
    }

    [Fact]
    public void Compare_WhenOnlyOneFileHasTrackPosition_NoComparisonPossible()
    {
        // Arrange: File3 has track at position 2, but no other file does
        var matrix = new List<List<bool>>
        {
            new() { true },               // File1: 1 track
            new() { true, false },        // File2: 2 tracks
            new() { true, false, true }   // File3: 3 tracks — position 2 has no other file to compare against
        };
        var files = new List<ScannedFileInfo>
        {
            CreateFileWithAudioTracks("file1.mkv", 1),
            CreateFileWithAudioTracks("file2.mkv", 2),
            CreateFileWithAudioTracks("file3.mkv", 3)
        };

        // Act
        var results = RollingReferenceComparer.Compare(matrix, files, TrackType.Audio, ValidationSeverity.Error, "Default flag").ToList();

        // Assert: No mismatches — overlapping positions all match, position 2 has no comparison
        Assert.Empty(results);
    }

    [Fact]
    public void Compare_RollingReference_MultipleFilesWithSameExtraTrack()
    {
        // Arrange:
        // File1: [track0]                    <- Reference for position 0
        // File2: [track0, track1]            <- Reference for position 1
        // File3: [track0, track1]            <- Compared against File2 for position 1
        // File2.track1 = false, File3.track1 = true → mismatch
        var matrix = new List<List<bool>>
        {
            new() { true },            // File1
            new() { true, false },     // File2 — reference for position 1
            new() { true, true }       // File3 — position 1 differs from File2
        };
        var files = new List<ScannedFileInfo>
        {
            CreateFileWithAudioTracks("file1.mkv", 1),
            CreateFileWithAudioTracks("file2.mkv", 2),
            CreateFileWithAudioTracks("file3.mkv", 2)
        };

        // Act
        var results = RollingReferenceComparer.Compare(matrix, files, TrackType.Audio, ValidationSeverity.Error, "Default flag").ToList();

        // Assert
        Assert.Single(results);
        Assert.Contains("position 2", results[0].Message);
        Assert.Equal("file3.mkv", results[0].ValidatedFilePath);
        Assert.Contains("file2.mkv", results[0].Message);
    }

    [Fact]
    public void Compare_RollingReference_WorksWithStringValues()
    {
        // Arrange: Same rolling reference logic with string (language) values
        var matrix = new List<List<string>>
        {
            new() { "eng" },                // File1: reference for position 0
            new() { "eng", "jpn" },         // File2: reference for position 1
            new() { "eng", "fra" }          // File3: position 1 differs from File2
        };
        var files = new List<ScannedFileInfo>
        {
            CreateFileWithAudioTracks("file1.mkv", 1),
            CreateFileWithAudioTracks("file2.mkv", 2),
            CreateFileWithAudioTracks("file3.mkv", 2)
        };

        // Act
        var results = RollingReferenceComparer.Compare(matrix, files, TrackType.Audio, ValidationSeverity.Warning, "Language").ToList();

        // Assert
        Assert.Single(results);
        Assert.Contains("Language", results[0].Message);
        Assert.Contains("'jpn'", results[0].Message);
        Assert.Contains("'fra'", results[0].Message);
    }

    [Fact]
    public void Compare_RollingReference_ReportsAllMismatchesAcrossPositions()
    {
        // Arrange:
        // File1: [A]               <- Reference for position 0
        // File2: [B, C]            <- Mismatch at 0 vs File1, reference for position 1
        // File3: [A, D]            <- Match at 0 vs File1, mismatch at 1 vs File2
        var matrix = new List<List<string>>
        {
            new() { "A" },
            new() { "B", "C" },
            new() { "A", "D" }
        };
        var files = new List<ScannedFileInfo>
        {
            CreateFileWithAudioTracks("file1.mkv", 1),
            CreateFileWithAudioTracks("file2.mkv", 2),
            CreateFileWithAudioTracks("file3.mkv", 2)
        };

        // Act
        var results = RollingReferenceComparer.Compare(matrix, files, TrackType.Audio, ValidationSeverity.Error, "Language").ToList();

        // Assert: 2 mismatches total
        Assert.Equal(2, results.Count);
        // Position 0: File2 vs File1
        Assert.Contains(results, r => r.ValidatedFilePath == "file2.mkv" && r.Message.Contains("position 1"));
        // Position 1: File3 vs File2
        Assert.Contains(results, r => r.ValidatedFilePath == "file3.mkv" && r.Message.Contains("position 2"));
    }

    [Fact]
    public void Compare_WhenReferenceFileHasMoreTracksThanSubsequent_ValidatesOverlappingOnly()
    {
        // Arrange: Reference has MORE tracks than subsequent files
        var matrix = new List<List<bool>>
        {
            new() { true, false, true },  // File1: 3 tracks (reference)
            new() { false, true }         // File2: 2 tracks — overlapping positions 0-1 both differ
        };
        var files = new List<ScannedFileInfo>
        {
            CreateFileWithAudioTracks("file1.mkv", 3),
            CreateFileWithAudioTracks("file2.mkv", 2)
        };

        // Act
        var results = RollingReferenceComparer.Compare(matrix, files, TrackType.Audio, ValidationSeverity.Error, "Default flag").ToList();

        // Assert: Mismatches at overlapping positions 0 and 1
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("file2.mkv", r.ValidatedFilePath));
    }

    private static ScannedFileInfo CreateFileWithAudioTracks(string path, int trackCount)
    {
        var builder = new MediaInfoResultBuilder();
        for (int i = 0; i < trackCount; i++)
        {
            builder.AddTrack(new TrackInfoBuilder()
                .WithType(TrackType.Audio)
                .WithStreamKindID(i)
                .Build());
        }
        return new ScannedFileInfo(builder.Build(), path);
    }
}
