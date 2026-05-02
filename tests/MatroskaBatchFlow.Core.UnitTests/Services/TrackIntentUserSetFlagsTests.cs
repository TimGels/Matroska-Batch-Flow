using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;
using MatroskaBatchFlow.Core.UnitTests.Builders;

namespace MatroskaBatchFlow.Core.UnitTests.Services;

public class TrackIntentUserSetFlagsTests
{
    [Fact]
    public void DefaultsAreFalse()
    {
        var intent = CreateIntent();

        Assert.False(intent.IsNameUserSet);
        Assert.False(intent.IsLanguageUserSet);
        Assert.False(intent.IsDefaultUserSet);
        Assert.False(intent.IsForcedUserSet);
        Assert.False(intent.IsEnabledUserSet);
    }

    [Theory]
    [InlineData(TrackIntentProperty.Name)]
    [InlineData(TrackIntentProperty.Language)]
    [InlineData(TrackIntentProperty.Default)]
    [InlineData(TrackIntentProperty.Forced)]
    [InlineData(TrackIntentProperty.Enabled)]
    public void MarkUserSet_SetsCorrespondingFlag_AndRaisesPropertyChanged(TrackIntentProperty property)
    {
        var intent = CreateIntent();
        var raised = new List<string?>();
        intent.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        intent.MarkUserSet(property);

        Assert.True(GetFlag(intent, property));
        Assert.Contains(GetFlagPropertyName(property), raised);
    }

    [Fact]
    public void MarkUserSet_IsIdempotent()
    {
        var intent = CreateIntent();
        intent.MarkUserSet(TrackIntentProperty.Name);

        var raised = new List<string?>();
        intent.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        intent.MarkUserSet(TrackIntentProperty.Name);

        Assert.Empty(raised);
    }

    [Fact]
    public void ResetUserSet_WithProperty_ClearsOnlyThatFlag()
    {
        var intent = CreateIntent();
        intent.MarkUserSet(TrackIntentProperty.Name);
        intent.MarkUserSet(TrackIntentProperty.Language);

        intent.ResetUserSet(TrackIntentProperty.Name);

        Assert.False(intent.IsNameUserSet);
        Assert.True(intent.IsLanguageUserSet);
    }

    [Fact]
    public void ResetUserSet_Null_ClearsAllFlags()
    {
        var intent = CreateIntent();
        intent.MarkUserSet(TrackIntentProperty.Name);
        intent.MarkUserSet(TrackIntentProperty.Language);
        intent.MarkUserSet(TrackIntentProperty.Default);
        intent.MarkUserSet(TrackIntentProperty.Forced);
        intent.MarkUserSet(TrackIntentProperty.Enabled);

        intent.ResetUserSet();

        Assert.False(intent.IsNameUserSet);
        Assert.False(intent.IsLanguageUserSet);
        Assert.False(intent.IsDefaultUserSet);
        Assert.False(intent.IsForcedUserSet);
        Assert.False(intent.IsEnabledUserSet);
    }

    private static TrackIntent CreateIntent()
    {
        var trackInfo = new TrackInfoBuilder().WithType(TrackType.Audio).WithStreamKindID(0).Build();
        return new TrackIntent(trackInfo) { Type = TrackType.Audio, Index = 0 };
    }

    private static bool GetFlag(TrackIntent intent, TrackIntentProperty property) => property switch
    {
        TrackIntentProperty.Name => intent.IsNameUserSet,
        TrackIntentProperty.Language => intent.IsLanguageUserSet,
        TrackIntentProperty.Default => intent.IsDefaultUserSet,
        TrackIntentProperty.Forced => intent.IsForcedUserSet,
        TrackIntentProperty.Enabled => intent.IsEnabledUserSet,
        _ => throw new ArgumentOutOfRangeException(nameof(property)),
    };

    private static string GetFlagPropertyName(TrackIntentProperty property) => property switch
    {
        TrackIntentProperty.Name => nameof(TrackIntent.IsNameUserSet),
        TrackIntentProperty.Language => nameof(TrackIntent.IsLanguageUserSet),
        TrackIntentProperty.Default => nameof(TrackIntent.IsDefaultUserSet),
        TrackIntentProperty.Forced => nameof(TrackIntent.IsForcedUserSet),
        TrackIntentProperty.Enabled => nameof(TrackIntent.IsEnabledUserSet),
        _ => throw new ArgumentOutOfRangeException(nameof(property)),
    };
}
