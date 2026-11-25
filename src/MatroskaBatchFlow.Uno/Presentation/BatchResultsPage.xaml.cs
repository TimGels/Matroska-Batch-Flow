using MatroskaBatchFlow.Uno.Models;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Windows.ApplicationModel.DataTransfer;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Represents a page for displaying batch processing results.
/// </summary>
public sealed partial class BatchResultsPage : Page
{
    private Func<Task>? _pendingAction;

    public BatchResultsViewModel ViewModel { get; }

    public BatchResultsPage()
    {
        ViewModel = App.GetService<BatchResultsViewModel>();
        this.InitializeComponent();
        DataContext = ViewModel;
    }

    /// <summary>
    /// Gets the severity of the outcome banner.
    /// </summary>
    /// <param name="outcome">The overall result of the batch.</param>
    /// <returns>The severity that matches the outcome.</returns>
    private InfoBarSeverity Severity(BatchOutcome outcome) => outcome switch
    {
        BatchOutcome.Succeeded => InfoBarSeverity.Success,
        BatchOutcome.Warnings or BatchOutcome.Canceled => InfoBarSeverity.Warning,
        BatchOutcome.Failures or BatchOutcome.Interrupted => InfoBarSeverity.Error,
        _ => InfoBarSeverity.Informational
    };

    private void OnRunFlyoutOpening(object sender, object args)
    {
        ShowPickerList();
        if (ViewModel.SelectedRun is { } run)
        {
            RunList.ScrollIntoView(run);
        }
    }

    private void OnRunFlyoutClosing(FlyoutBase sender, FlyoutBaseClosingEventArgs args) =>
        args.Cancel = ViewModel.IsChangingHistory;

    private void OnRunPicked(object sender, ItemClickEventArgs args)
    {
        if (_pendingAction is not null || args.ClickedItem is not BatchRunSummary run)
            return;

        ViewModel.SelectedRun = run;
        RunFlyout.Hide();
    }

    private void OnPickerSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        // Automation can select a row without raising ItemClick.
        if (RunFlyout.IsOpen && _pendingAction is null && !ViewModel.IsChangingHistory
            && ((ListView)sender).SelectedItem is BatchRunSummary run && run != ViewModel.SelectedRun)
        {
            ViewModel.SelectedRun = run;
            RunFlyout.Hide();
        }
    }

    private void OnPickerContainerChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (!args.InRecycleQueue && args.Item is BatchRunSummary run)
        {
            args.ItemContainer.SetBinding(AutomationProperties.NameProperty, new Binding
            {
                Source = run,
                Path = new PropertyPath(nameof(BatchRunSummary.PickerLabel)),
                Mode = BindingMode.OneWay
            });
        }
    }

    private void OnRemoveRunClick(object sender, RoutedEventArgs args)
    {
        if (ViewModel.HasUnreadableHistory)
        {
            ResetHistoryButton.Focus(FocusState.Programmatic);
            return;
        }

        var run = (BatchRunSummary)((Button)sender).Tag;
        var removal = new HistoryRemoval([run.Run.Id]);
        ShowConfirmation(UiText.Get("HistoryConfirmRemoveSingle"), UiText.Get("HistoryRemove"),
            () => RemoveResultsAsync(removal), run.PickerLabel);
    }

    private void OnClearHistoryClick(object sender, RoutedEventArgs args)
    {
        if (!ViewModel.CanClearResultHistory)
            return;

        var removal = ViewModel.CaptureListedResults();
        ShowConfirmation(UiText.Counted("HistoryConfirmClear", removal.RunIds.Count), UiText.Get("HistoryRemoveAll"),
            () => RemoveResultsAsync(removal));
    }

    private void OnResetHistoryClick(object sender, RoutedEventArgs args) =>
        ShowConfirmation(UiText.Get("HistoryConfirmReset"), UiText.Get("HistoryReset"), ResetHistoryAsync);

    private void ShowConfirmation(string question, string button, Func<Task> action, string? detail = null)
    {
        _pendingAction = action;
        PickerError.IsOpen = HistoryFeedback.IsOpen = false;
        ConfirmationQuestion.Text = question;
        ConfirmRemoval.Content = button;
        RemovalDescription.Text = detail ?? string.Empty;
        RemovalDescription.Visibility = detail is null ? Visibility.Collapsed : Visibility.Visible;
        var prompt = detail is null ? question : string.Join(Environment.NewLine, question, detail);
        AutomationProperties.SetName(CancelRemoval, UiText.Format("HistoryConfirmationActionName", UiText.Get("HistoryCancel"), prompt));
        AutomationProperties.SetName(ConfirmRemoval, UiText.Format("HistoryConfirmationActionName", button, prompt));
        PickerBrowse.Visibility = Visibility.Collapsed;
        PickerConfirmation.Visibility = Visibility.Visible;
        CancelRemoval.Focus(FocusState.Programmatic);
    }

    private void OnCancelRemoval(object sender, RoutedEventArgs args)
    {
        ShowPickerList();
        Control target = ViewModel.HasUnreadableHistory ? ResetHistoryButton : RunList;
        target.Focus(FocusState.Programmatic);
    }

    private async void OnConfirmHistoryAction(object sender, RoutedEventArgs args)
    {
        if (_pendingAction is not { } action || ViewModel.IsChangingHistory)
            return;

        PickerError.IsOpen = false;
        CancelRemoval.IsEnabled = ConfirmRemoval.IsEnabled = false;
        try
        {
            await action();
        }
        finally
        {
            CancelRemoval.IsEnabled = ConfirmRemoval.IsEnabled = true;
            if (PickerError.IsOpen)
            {
                CancelRemoval.Focus(FocusState.Programmatic);
            }
        }
    }

    private async Task RemoveResultsAsync(HistoryRemoval removal)
    {
        var result = await ViewModel.RemoveHistoryAsync(removal);
        if (result.Status == HistoryRemovalStatus.Removed)
        {
            FinishHistoryAction(UiText.Counted("HistoryRemoved", result.RemovedCount));
        }
        else if (ViewModel.HasUnreadableHistory)
        {
            ShowPickerList();
            ResetHistoryButton.Focus(FocusState.Programmatic);
        }
        else
        {
            ShowPickerError(UiText.Get("HistoryRemovalFailed"));
        }
    }

    private async Task ResetHistoryAsync()
    {
        if (await ViewModel.ResetUnreadableHistoryAsync())
        {
            FinishHistoryAction(UiText.Get("HistoryRecoverySucceeded"));
        }
        else
        {
            ShowPickerError(UiText.Get("HistoryRecoveryFailed"));
        }
    }

    private void FinishHistoryAction(string message)
    {
        RunFlyout.Hide();
        HistoryFeedback.Message = message;
        HistoryFeedback.IsOpen = true;
    }

    private void ShowPickerList()
    {
        _pendingAction = null;
        PickerConfirmation.Visibility = Visibility.Collapsed;
        PickerBrowse.Visibility = Visibility.Visible;
        RunList.SelectedItem = ViewModel.SelectedRun;
        PickerError.IsOpen = false;
    }

    private void ShowPickerError(string message)
    {
        PickerError.Message = message;
        PickerError.IsOpen = true;
    }

    private void OnCopyCommandClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string command)
            return;

        var dataPackage = new DataPackage();
        dataPackage.SetText(command);
        Clipboard.SetContent(dataPackage);
    }
}
