using System.Diagnostics;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MKVBatchFlow.Uno.Presentation;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class InputPage : Page
{
    public InputPage()
    {
        this.InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) 
        => Debug.WriteLine("LOADED");
    private void OnUnloaded(object sender, RoutedEventArgs e) 
        => Debug.WriteLine("UNLOADED");
}
