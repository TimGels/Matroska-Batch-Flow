using MatroskaBatchFlow.Uno.Logging;
using Microsoft.UI.Xaml.Media.Animation;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// LoggerMessage definitions for <see cref="NavigationService"/>.
/// </summary>
public partial class NavigationService
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Navigating to {PageKey}, ClearNavigation: {ClearNavigation} TransitionInfo: {TransitionInfo}")]
    private partial void LogNavigatingTo(string pageKey, bool clearNavigation, NavigationTransitionInfo? transitionInfo);
}
