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
            ErrorDialog.ViewModel.Title = m.Title;
            ErrorDialog.ViewModel.Message = m.Message;
            await ErrorDialog.ShowAsync();
        });
    }
}
