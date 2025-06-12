using CommunityToolkit.Mvvm.Messaging;
using MatroskaBatchFlow.Uno.Presentation.Dialogs;

namespace MatroskaBatchFlow.Uno.Presentation;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();

        WeakReferenceMessenger.Default.Register<DialogMessage>(this, async (r, m) =>
        {
            await new ErrorDialog
            {
                ViewModel =
                {
                    Title = m.Title,
                    Message = m.Message
                },
                XamlRoot = XamlRoot,
            }.ShowAsync();
        });
    }
}
