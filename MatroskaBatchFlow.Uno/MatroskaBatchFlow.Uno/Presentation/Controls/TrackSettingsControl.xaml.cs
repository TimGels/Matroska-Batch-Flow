using MatroskaBatchFlow.Core;
using MatroskaBatchFlow.Core.Models;
using System.Collections.Immutable;

namespace MatroskaBatchFlow.Uno.Presentation.Controls;

public sealed partial class TrackSettingsControl : UserControl
{
    public TrackSettingsControl()
    {
        this.InitializeComponent();
    }

    public static readonly DependencyProperty SelectedTrackProperty =
        DependencyProperty.Register(
            nameof(SelectedTrack),
            typeof(TrackConfiguration),
            typeof(TrackSettingsControl),
            new PropertyMetadata(default));

    public TrackConfiguration SelectedTrack
    {
        get => (TrackConfiguration)GetValue(SelectedTrackProperty);
        set => SetValue(SelectedTrackProperty, value);
    }

    public static readonly DependencyProperty TracksProperty =
        DependencyProperty.Register(
            nameof(Tracks),
            typeof(IList<TrackConfiguration>),
            typeof(TrackSettingsControl),
            new PropertyMetadata(null));

    public IList<TrackConfiguration> Tracks
    {
        get => (IList<TrackConfiguration>)GetValue(TracksProperty);
        set => SetValue(TracksProperty, value);
    }

    public static readonly DependencyProperty AvailableLanguagesProperty =
        DependencyProperty.Register(
            nameof(AvailableLanguages),
            typeof(ImmutableList<MatroskaLanguageOption>),
            typeof(TrackSettingsControl),
            new PropertyMetadata(default(ImmutableList<MatroskaLanguageOption>)));

    public ImmutableList<MatroskaLanguageOption> AvailableLanguages
    {
        get => (ImmutableList<MatroskaLanguageOption>)GetValue(AvailableLanguagesProperty);
        set => SetValue(AvailableLanguagesProperty, value);
    }

    private void DefaultYesRadioButton_Checked(object sender, RoutedEventArgs e)
    {

    }    
    
    private void DefaultNoRadioButton_Checked(object sender, RoutedEventArgs e)
    {

    }
}
