using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.UnitTests.Enums;

/// <summary>
/// Contains unit tests for the ValidationSeverity enum.
/// </summary>
public class ValidationSeverityTests
{
    [Fact]
    public void ValidationSeverity_HasExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)ValidationSeverity.Off);
        Assert.Equal(1, (int)ValidationSeverity.Info);
        Assert.Equal(2, (int)ValidationSeverity.Warning);
        Assert.Equal(3, (int)ValidationSeverity.Error);
    }

    [Theory]
    [InlineData(ValidationSeverity.Off)]
    [InlineData(ValidationSeverity.Info)]
    [InlineData(ValidationSeverity.Warning)]
    [InlineData(ValidationSeverity.Error)]
    public void ValidationSeverity_CanBeAssignedAndCompared(ValidationSeverity severity)
    {
        // Act
        var assigned = severity;

        // Assert
        Assert.Equal(severity, assigned);
    }

    [Fact]
    public void ValidationSeverity_IsOrdered()
    {
        // Assert - Verify severity ordering (Off < Info < Warning < Error)
        Assert.True(ValidationSeverity.Off < ValidationSeverity.Info);
        Assert.True(ValidationSeverity.Info < ValidationSeverity.Warning);
        Assert.True(ValidationSeverity.Warning < ValidationSeverity.Error);
    }
}
