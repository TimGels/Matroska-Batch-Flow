using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.UnitTests.Enums;

/// <summary>
/// Contains unit tests for the StrictnessMode enum.
/// </summary>
public class StrictnessModeTests
{
    [Fact]
    public void StrictnessMode_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)StrictnessMode.Strict);
        Assert.Equal(1, (int)StrictnessMode.Lenient);
        Assert.Equal(2, (int)StrictnessMode.Custom);
    }

    [Theory]
    [InlineData(StrictnessMode.Strict)]
    [InlineData(StrictnessMode.Lenient)]
    [InlineData(StrictnessMode.Custom)]
    public void StrictnessMode_CanBeAssignedAndCompared(StrictnessMode mode)
    {
        // Act
        var assigned = mode;

        // Assert
        Assert.Equal(mode, assigned);
    }
}
