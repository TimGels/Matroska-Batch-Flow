using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.UnitTests.Models;

/// <summary>
/// Contains unit tests for the MkvPropeditResult model.
/// </summary>
public class MkvPropeditResultTests
{
    [Fact]
    public void MkvPropeditResult_CanBeConstructed_WithRequiredProperties()
    {
        // Arrange & Act
        var result = new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Success,
            Warnings = []
        };

        // Assert
        Assert.Equal(MkvPropeditStatus.Success, result.Status);
        Assert.Empty(result.Warnings);
        Assert.Null(result.StandardOutput);
        Assert.Null(result.StandardError);
        Assert.Equal(string.Empty, result.ResolvedExecutablePath);
        Assert.Equal(string.Empty, result.ExecutableArguments);
    }

    [Fact]
    public void MkvPropeditResult_CanBeConstructed_WithAllProperties()
    {
        // Arrange
        var warnings = new List<string> { "Warning 1", "Warning 2" };

        // Act
        var result = new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Warning,
            StandardOutput = "Output text",
            StandardError = "Error text",
            Warnings = warnings,
            ResolvedExecutablePath = "C:\\Tools\\mkvpropedit.exe",
            ExecutableArguments = "--edit track:a1 --set flag-default=1 file.mkv"
        };

        // Assert
        Assert.Equal(MkvPropeditStatus.Warning, result.Status);
        Assert.Equal("Output text", result.StandardOutput);
        Assert.Equal("Error text", result.StandardError);
        Assert.Equal(warnings, result.Warnings);
        Assert.Equal("C:\\Tools\\mkvpropedit.exe", result.ResolvedExecutablePath);
        Assert.Equal("--edit track:a1 --set flag-default=1 file.mkv", result.ExecutableArguments);
    }

    [Theory]
    [InlineData(MkvPropeditStatus.Success, false)]
    [InlineData(MkvPropeditStatus.Warning, false)]
    [InlineData(MkvPropeditStatus.Error, true)]
    [InlineData(MkvPropeditStatus.Unknown, true)]
    public void IsFatal_ReturnsCorrectValue_BasedOnStatus(MkvPropeditStatus status, bool expectedIsFatal)
    {
        // Arrange
        var result = new MkvPropeditResult
        {
            Status = status,
            Warnings = []
        };

        // Act
        var isFatal = result.IsFatal;

        // Assert
        Assert.Equal(expectedIsFatal, isFatal);
    }

    [Fact]
    public void SimulatedCommandLine_ReturnsCombinedString_WhenBothPathAndArgumentsAreProvided()
    {
        // Arrange
        var result = new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Success,
            Warnings = [],
            ResolvedExecutablePath = "C:\\Tools\\mkvpropedit.exe",
            ExecutableArguments = "--edit track:a1 --set flag-default=1 file.mkv"
        };

        // Act
        var commandLine = result.SimulatedCommandLine;

        // Assert
        Assert.Equal("C:\\Tools\\mkvpropedit.exe --edit track:a1 --set flag-default=1 file.mkv", commandLine);
    }

    [Fact]
    public void SimulatedCommandLine_ReturnsExecutablePath_WhenOnlyPathIsProvided()
    {
        // Arrange
        var result = new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Success,
            Warnings = [],
            ResolvedExecutablePath = "C:\\Tools\\mkvpropedit.exe",
            ExecutableArguments = string.Empty
        };

        // Act
        var commandLine = result.SimulatedCommandLine;

        // Assert
        Assert.Equal("C:\\Tools\\mkvpropedit.exe", commandLine);
    }

    [Fact]
    public void SimulatedCommandLine_ReturnsArguments_WhenOnlyArgumentsAreProvided()
    {
        // Arrange
        var result = new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Success,
            Warnings = [],
            ResolvedExecutablePath = string.Empty,
            ExecutableArguments = "--edit track:a1 --set flag-default=1 file.mkv"
        };

        // Act
        var commandLine = result.SimulatedCommandLine;

        // Assert
        Assert.Equal("--edit track:a1 --set flag-default=1 file.mkv", commandLine);
    }

    [Fact]
    public void SimulatedCommandLine_ReturnsEmptyString_WhenBothPathAndArgumentsAreEmpty()
    {
        // Arrange
        var result = new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Success,
            Warnings = [],
            ResolvedExecutablePath = string.Empty,
            ExecutableArguments = string.Empty
        };

        // Act
        var commandLine = result.SimulatedCommandLine;

        // Assert
        Assert.Equal(string.Empty, commandLine);
    }

    [Fact]
    public void Warnings_CanBeEmptyCollection()
    {
        // Arrange & Act
        var result = new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Success,
            Warnings = []
        };

        // Assert
        Assert.NotNull(result.Warnings);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Warnings_CanContainMultipleItems()
    {
        // Arrange
        var warnings = new List<string>
        {
            "Warning: Track language not set",
            "Warning: Missing default flag",
            "Warning: Codec delay detected"
        };

        // Act
        var result = new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Warning,
            Warnings = warnings
        };

        // Assert
        Assert.Equal(3, result.Warnings.Count);
        Assert.Equal("Warning: Track language not set", result.Warnings[0]);
        Assert.Equal("Warning: Missing default flag", result.Warnings[1]);
        Assert.Equal("Warning: Codec delay detected", result.Warnings[2]);
    }

    [Fact]
    public void MkvPropeditResult_IsRecord_WarningsUsesReferenceEquality()
    {
        // IReadOnlyList<string> has no structural equality; record equality for Warnings
        // falls back to reference equality. Two records that are identical in every way
        // but use different list instances are therefore not equal.
        var result1 = new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Success,
            StandardOutput = "Output",
            StandardError = "Error",
            Warnings = ["Warning 1"],
            ResolvedExecutablePath = "path.exe",
            ExecutableArguments = "args"
        };

        var result2 = new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Success,
            StandardOutput = "Output",
            StandardError = "Error",
            Warnings = ["Warning 1"],   // same contents, different instance
            ResolvedExecutablePath = "path.exe",
            ExecutableArguments = "args"
        };

        // Act & Assert
        Assert.NotEqual(result1, result2);
        Assert.False(result1 == result2);
    }

    /// <summary>
    /// Verifies that the with-expression produces a new record instance (non-destructive mutation),
    /// and that unchanged members — including reference-type members like Warnings — are shallow-copied.
    /// This means the new record gets a copy of the list <em>reference</em>, not a copy of the list itself.
    /// Consumers must not assume that with provides full isolation for mutable reference-type members.
    /// </summary>  
    [Fact]
    public void MkvPropeditResult_IsRecord_SupportsWith_ForImmutableUpdates()
    {
        // Arrange
        var originalWarnings = new List<string> { "Warning 1" };
        var original = new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Warning,
            Warnings = originalWarnings,
            ResolvedExecutablePath = "original.exe"
        };

        // Act
        var updated = original with { Status = MkvPropeditStatus.Success };

        // Assert - with produces a new record instance
        Assert.NotSame(original, updated);

        // Unchanged members are copied to the new instance
        Assert.Equal(MkvPropeditStatus.Warning, original.Status);
        Assert.Equal(MkvPropeditStatus.Success, updated.Status);
        Assert.Equal("original.exe", updated.ResolvedExecutablePath);

        // Warnings is a reference type; with performs a shallow copy, so both records
        // reference the same underlying list instance.
        Assert.Same(originalWarnings, updated.Warnings);
    }

    [Fact]
    public void StandardOutput_CanBeNull()
    {
        // Arrange & Act
        var result = new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Success,
            Warnings = [],
            StandardOutput = null
        };

        // Assert
        Assert.Null(result.StandardOutput);
    }

    [Fact]
    public void StandardError_CanBeNull()
    {
        // Arrange & Act
        var result = new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Error,
            Warnings = [],
            StandardError = null
        };

        // Assert
        Assert.Null(result.StandardError);
    }
}
