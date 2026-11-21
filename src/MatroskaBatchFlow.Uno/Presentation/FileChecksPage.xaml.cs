using MatroskaBatchFlow.Uno.Models;

namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class FileChecksPage : Page
{
    private FileCheckKey? _displayedTopic;
    public FileChecksViewModel ViewModel { get; }

    public FileChecksPage()
    {
        ViewModel = App.GetService<FileChecksViewModel>();
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
