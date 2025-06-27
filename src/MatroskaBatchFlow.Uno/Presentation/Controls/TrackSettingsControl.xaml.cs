using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Services;

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
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsDefaultTrackProperty =
        DependencyProperty.Register(
            "IsDefaultTrack",
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsForcedTrackProperty =
        DependencyProperty.Register(
            "IsForcedTrack",
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(false));

    public static readonly DependencyProperty IsEnabledTrackProperty =
        DependencyProperty.Register(
            "IsEnabledTrack",
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(false));

    public static readonly DependencyProperty TrackNameProperty =
        DependencyProperty.Register(
            "TrackName",
            typeof(string),
            typeof(TrackSettingsControl),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ChangeTrackNameProperty =
        DependencyProperty.Register(
            "ChangeTrackName",
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(false));

    public static readonly DependencyProperty AvailableLanguagesProperty =
        DependencyProperty.Register(
            nameof(AvailableLanguages),
            typeof(ImmutableList<MatroskaLanguageOption>),
            typeof(TrackSettingsControl),
            new PropertyMetadata(default(ImmutableList<MatroskaLanguageOption>)));

    public static readonly DependencyProperty SelectedLanguageProperty =
        DependencyProperty.Register(
            nameof(SelectedLanguage),
            typeof(MatroskaLanguageOption),
            typeof(TrackSettingsControl),
            new PropertyMetadata(null));

    public bool IsDefaultTrack
    {
        get => (bool)GetValue(IsDefaultTrackProperty);
        set => SetValue(IsDefaultTrackProperty, value);
    }

    public bool IsForcedTrack
    {
        get => (bool)GetValue(IsForcedTrackProperty);
        set => SetValue(IsForcedTrackProperty, value);
    }

    public bool IsEnabledTrack
    {
        get => (bool)GetValue(IsEnabledTrackProperty);
        set => SetValue(IsEnabledTrackProperty, value);
    }

    public string TrackName
    {
        get => (string)GetValue(TrackNameProperty);
        set => SetValue(TrackNameProperty, value);
    }

    public TrackConfiguration SelectedTrack
    {
        get => (TrackConfiguration)GetValue(SelectedTrackProperty);
        set => SetValue(SelectedTrackProperty, value);
    }

    public ImmutableList<MatroskaLanguageOption> AvailableLanguages
    {
        get => (ImmutableList<MatroskaLanguageOption>)GetValue(AvailableLanguagesProperty);
        set => SetValue(AvailableLanguagesProperty, value);
    }

    public MatroskaLanguageOption? SelectedLanguage
    {
        get => (MatroskaLanguageOption?)GetValue(SelectedLanguageProperty);
        set => SetValue(SelectedLanguageProperty, value);
    }

    private void DefaultYesRadioButton_Checked(object sender, RoutedEventArgs e)
    {

    }

    private void DefaultNoRadioButton_Checked(object sender, RoutedEventArgs e)
    {

    }
}
