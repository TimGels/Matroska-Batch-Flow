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
    public void MkvPropeditResult_IsRecord_SupportsValueEquality()
    {
        // Arrange
        var warnings = new List<string> { "Warning 1" };
        var result1 = new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Success,
            StandardOutput = "Output",
            StandardError = "Error",
            Warnings = warnings,
            ResolvedExecutablePath = "path.exe",
            ExecutableArguments = "args"
        };

        var result2 = new MkvPropeditResult
        {
            Status = MkvPropeditStatus.Success,
            StandardOutput = "Output",
            StandardError = "Error",
            Warnings = warnings,
            ResolvedExecutablePath = "path.exe",
            ExecutableArguments = "args"
        };

        // Act & Assert
        Assert.Equal(result1, result2);
        Assert.True(result1 == result2);
    }

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

        // Assert
        Assert.Equal(MkvPropeditStatus.Warning, original.Status);
        Assert.Equal(MkvPropeditStatus.Success, updated.Status);
        Assert.Equal("original.exe", updated.ResolvedExecutablePath);
        Assert.Equal(originalWarnings, updated.Warnings);
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
