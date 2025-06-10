using CommunityToolkit.Mvvm.ComponentModel;

namespace MatroskaBatchFlow.Uno.Presentation.Dialogs;

public partial class ErrorDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string message = string.Empty;
}
