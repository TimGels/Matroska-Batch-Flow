using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// The Validation page, which reviews the batch's file checks one check at a time.
/// </summary>
public sealed partial class ValidationPage : Page
{
    private FileCheckKey? _displayedTopic;

    /// <summary>
    /// Gets the view model of the page.
    /// </summary>
    public ValidationViewModel ViewModel { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationPage"/> class.
    /// </summary>
    public ValidationPage()
    {
        ViewModel = App.GetService<ValidationViewModel>();
        InitializeComponent();
    }

    private void OnCheckSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is not FileCheckGroup check || check.Topic == _displayedTopic)
            return;

        _displayedTopic = check.Topic;
        DetailsScroll?.ChangeView(null, 0, null, disableAnimation: true);
    }
}
