using MatroskaBatchFlow.Core.Services.Processing;

namespace MatroskaBatchFlow.Core.UnitTests.Services.Processing;

/// <summary>
/// Contains unit tests for the <see cref="MkvPropeditOutputParser"/> class.
/// </summary>
public class MkvPropeditOutputParserTests
{
    [Theory]
    [InlineData("Progress: 0%", 0)]
    [InlineData("Progress: 42%", 42)]
    [InlineData("Progress: 100%", 100)]
    [InlineData("Fortschritt: 7%", 7)]
    [InlineData("Progression : 55 %", 55)]
    [InlineData("进度：12%", 12)]
    public void TryParseProgress_ReturnsPercentage_ForProgressLines(string line, int expected)
    {
        var isProgress = MkvPropeditOutputParser.TryParseProgress(line, out var percent);

        Assert.True(isProgress);
        Assert.Equal(expected, percent);
    }

    [Theory]
    [InlineData("The file is being analyzed.")]
    [InlineData("The file is read in order to create track statistics.")]
    [InlineData("The changes are written to the file.")]
    [InlineData("Done.")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParseProgress_ReturnsFalse_ForOtherLines(string? line)
    {
        Assert.False(MkvPropeditOutputParser.TryParseProgress(line, out _));
    }

    [Fact]
    public void RemoveProgressLines_KeepsOnlyNonProgressLines()
    {
        var output = string.Join(Environment.NewLine,
            "The file is being analyzed.",
            "Progress: 0%",
            "Progress: 50%",
            "Progress: 100%",
            "Done.");

        var result = MkvPropeditOutputParser.RemoveProgressLines(output);

        Assert.Equal($"The file is being analyzed.{Environment.NewLine}Done.", result);
    }
}
