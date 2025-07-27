using System.Diagnostics.CodeAnalysis;
using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Contracts.ViewModels;
using MatroskaBatchFlow.Uno.Extensions;
using Microsoft.UI.Xaml.Media.Animation;

namespace MatroskaBatchFlow.Uno.Services;

public class NavigationService(IPageService pageService) : INavigationService
{
    private object? _lastParameterUsed;
    private Frame? _frame;

    public event NavigatedEventHandler? Navigated;

    public Frame? Frame
    {
        get
        {
            if (_frame == null)
            {
                _frame = App.MainWindow.Content as Frame;
                RegisterFrameEvents();
            }

            return _frame;
        }

        set
        {
            UnregisterFrameEvents();
            _frame = value;
            RegisterFrameEvents();
        }
    }

    [MemberNotNullWhen(true, nameof(Frame), nameof(_frame))]
    public bool CanGoBack => Frame != null && Frame.CanGoBack;

    public void SetListDataItemForNextConnectedAnimation(object item)
    {
        //Frame.SetListDataItemForNextConnectedAnimation(item);
    }

    public bool GoBack()
    {
        if (CanGoBack)
        {
            var vmBeforeNavigation = _frame.GetPageViewModel();
            _frame.GoBack();
            if (vmBeforeNavigation is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedFrom();
            }

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    /// <remarks>The method will only navigate if the target page is different from the current page or if the
    /// parameter differs from the last used parameter.</remarks>
    public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false, NavigationTransitionInfo? transitionInfo = null)
    {
        if (_frame == null)
            return false;

        var pageType = pageService.GetPageType(pageKey);
        var frameContentType = _frame.Content?.GetType();
        bool isDifferentPage = frameContentType != pageType;
        bool isDifferentParameter = parameter != null && !parameter.Equals(_lastParameterUsed);
        bool shouldNavigate = isDifferentPage || isDifferentParameter;

        if (!shouldNavigate)
            return false;

        _frame.Tag = clearNavigation;

        bool navigated;
        if (transitionInfo != null)
            navigated = _frame.Navigate(pageType, parameter, transitionInfo);
        else
            navigated = _frame.Navigate(pageType, parameter);

        if (navigated)
        {
            var vmBeforeNavigation = _frame.GetPageViewModel();
            _lastParameterUsed = parameter;
            NotifyViewModelNavigatedFrom(vmBeforeNavigation);
        }

        return navigated;
    }

    /// <summary>
    /// Notifies the specified view model that a navigation event has occurred, indicating that the view model is being
    /// navigated away from. Requires the view model to implement the <see cref="INavigationAware"/> interface.
    /// </summary>
    /// <param name="viewModel">The view model to notify.</param>
    private static void NotifyViewModelNavigatedFrom(object? viewModel)
    {
        if (viewModel is INavigationAware navigationAware)
        {
            navigationAware.OnNavigatedFrom();
        }
    }

    private void OnNavigated(object sender, NavigationEventArgs e)
    {
        if (sender is Frame frame)
        {
            var clearNavigation = (bool)frame.Tag;
            if (clearNavigation)
            {
                frame.BackStack.Clear();
            }

            if (frame.GetPageViewModel() is INavigationAware navigationAware)
            {
                navigationAware.OnNavigatedTo(e.Parameter);
            }

            Navigated?.Invoke(sender, e);
        }
    }

    private void RegisterFrameEvents()
    {
        if (_frame != null)
        {
            _frame.Navigated += OnNavigated;
        }
    }

    private void UnregisterFrameEvents()
    {
        if (_frame != null)
        {
            _frame.Navigated -= OnNavigated;
        }
    }
}
