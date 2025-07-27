using Microsoft.UI.Xaml.Media.Animation;

namespace MatroskaBatchFlow.Uno.Contracts.Services;

public interface INavigationService
{
    event NavigatedEventHandler Navigated;

    bool CanGoBack { get; }

    Frame? Frame { get; set; }

    /// <summary>
    /// Navigates to the specified page using the provided page key.
    /// </summary>
    /// <param name="pageKey">The key that identifies the page to navigate to.</param>
    /// <param name="parameter">An optional parameter to pass to the target page. Can be <see langword="null"/>.</param>
    /// <param name="clearNavigation">If <see langword="true"/>, clears the navigation stack.</param>
    /// <param name="transitionInfo">An optional <see cref="NavigationTransitionInfo"/> to use for the navigation transition. Can be <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the navigation was successful; otherwise, <see langword="false"/>.</returns>
    bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false, NavigationTransitionInfo? transitionInfo = null);

    bool GoBack();

    void SetListDataItemForNextConnectedAnimation(object item);
}
