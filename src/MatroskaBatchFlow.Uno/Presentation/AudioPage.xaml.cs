using MatroskaBatchFlow.Core.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml.Shapes;

namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// Represents a page for audio-related track configuration.
/// </summary>
public sealed partial class AudioPage : Page
{
    public AudioViewModel ViewModel { get; }
    
    public AudioPage()
    {
        ViewModel = App.GetService<AudioViewModel>();
        this.InitializeComponent();
    }

    private void TrackAvailabilityText_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBlock textBlock && textBlock.Tag is int trackIndex)
        {
            textBlock.Text = ViewModel.GetTrackAvailabilityText(trackIndex);
            
            // Set tooltip with more detail
            ToolTipService.SetToolTip(textBlock, 
                $"Track {trackIndex + 1} is available in {ViewModel.GetTrackAvailabilityCount(trackIndex)} of {ViewModel.TotalFileCount} files");
        }
    }

    private void TrackAvailabilityDot_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (sender is not Ellipse ellipse || ellipse.DataContext is not TrackConfiguration track)
            return;

        int trackIndex = track.Index;
        int available = ViewModel.GetTrackAvailabilityCount(trackIndex);
        int total = ViewModel.TotalFileCount;
        
        // Set tooltip
        ToolTipService.SetToolTip(ellipse, 
            $"Track {trackIndex + 1} is available in {available} of {total} files");
        
        // Color code the dot based on availability
        if (total == 0 || available == 0)
        {
            ellipse.Fill = new SolidColorBrush(Colors.Gray);
            ellipse.Opacity = 0.3;
        }
        else if (available == total)
        {
            // Available in all files - use success color (green)
            ellipse.Fill = (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
            ellipse.Opacity = 1.0;
        }
        else
        {
            // Partial availability - use caution color (orange)
            ellipse.Fill = (Brush)Application.Current.Resources["SystemFillColorCautionBrush"];
            ellipse.Opacity = 1.0;
        }
    }
}
