using MatroskaBatchFlow.Uno.Models;
using Windows.ApplicationModel.DataTransfer;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Represents a page for displaying batch processing results.
/// </summary>
public sealed partial class BatchResultsPage : Page
{
    public BatchResultsViewModel ViewModel { get; }

    public BatchResultsPage()
    {
        this.InitializeComponent();
        ViewModel = App.GetService<BatchResultsViewModel>();
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
        BatchOutcome.Failures => InfoBarSeverity.Error,
        _ => InfoBarSeverity.Informational
    };

    private void OnCopyCommandClick(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string command)
            return;

        var dataPackage = new DataPackage();
        dataPackage.SetText(command);
        Clipboard.SetContent(dataPackage);
    }
}
