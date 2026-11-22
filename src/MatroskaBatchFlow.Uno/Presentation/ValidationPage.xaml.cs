using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class ValidationPage : Page
{
    private FileCheckKey? _displayedTopic;
    public ValidationViewModel ViewModel { get; }

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
