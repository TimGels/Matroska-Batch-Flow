// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.UI.Windowing;

namespace MatroskaBatchFlow.Uno.Presentation;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
    }
}
