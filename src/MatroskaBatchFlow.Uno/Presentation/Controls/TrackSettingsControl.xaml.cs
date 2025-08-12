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
            new PropertyMetadata(null));

    public static readonly DependencyProperty SelectedLanguageProperty =
        DependencyProperty.Register(
            nameof(SelectedLanguage),
            typeof(MatroskaLanguageOption),
            typeof(TrackSettingsControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ShouldModifyDefaultFlagProperty =
        DependencyProperty.Register(
            nameof(ShouldModifyDefaultFlag),
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShouldModifyForcedFlagProperty =
        DependencyProperty.Register(
            nameof(ShouldModifyForcedFlag),
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShouldModifyEnabledFlagProperty =
        DependencyProperty.Register(
            nameof(ShouldModifyEnabledFlag),
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShouldModifyNameProperty =
        DependencyProperty.Register(
            nameof(ShouldModifyName),
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ShouldModifyLanguageProperty =
        DependencyProperty.Register(
            nameof(ShouldModifyLanguage),
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(false));

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

    public bool ShouldModifyDefaultFlag
    {
        get => (bool)GetValue(ShouldModifyDefaultFlagProperty);
        set => SetValue(ShouldModifyDefaultFlagProperty, value);
    }

    public bool ShouldModifyForcedFlag
    {
        get => (bool)GetValue(ShouldModifyForcedFlagProperty);
        set => SetValue(ShouldModifyForcedFlagProperty, value);
    }

    public bool ShouldModifyEnabledFlag
    {
        get => (bool)GetValue(ShouldModifyEnabledFlagProperty);
        set => SetValue(ShouldModifyEnabledFlagProperty, value);
    }

    public bool ShouldModifyName
    {
        get => (bool)GetValue(ShouldModifyNameProperty);
        set => SetValue(ShouldModifyNameProperty, value);
    }

    public bool ShouldModifyLanguage
    {
        get => (bool)GetValue(ShouldModifyLanguageProperty);
        set => SetValue(ShouldModifyLanguageProperty, value);
    }

    private void DefaultYesRadioButton_Checked(object sender, RoutedEventArgs e)
    {

    }

    private void DefaultNoRadioButton_Checked(object sender, RoutedEventArgs e)
    {

    }

    /// <summary>
    /// Handles the TextSubmitted event for the LanguagesComboBox.
    /// </summary>
    /// <param name="sender">The ComboBox that triggered the event.</param>
    /// <param name="args">The event arguments containing the submitted text.</param>
    private void LanguagesComboBox_TextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs args)
    {
        // Try to find a matching language, or fall back to Undetermined.
        var selectedLanguage = FindLanguageOrUndetermined(args.Text);
        SelectedLanguage = selectedLanguage;

        // Prevent further processing of the event 
        // since we have handled the text submission.
        args.Handled = true; 
    }

    /// <summary>
    /// Finds a matching language from the available languages or returns the Undetermined option.
    /// </summary>
    /// <param name="input">The input string to match against available languages.</param>
    /// <returns>A <see cref="MatroskaLanguageOption"/> that matches the input, or the Undetermined option if no match is found.</returns>
    private MatroskaLanguageOption FindLanguageOrUndetermined(string? input)
    {
        if (string.IsNullOrWhiteSpace(input) || AvailableLanguages is null)
            return MatroskaLanguageOption.Undetermined;

        return AvailableLanguages.FirstOrDefault(lang =>
            string.Equals(lang.Name, input, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(lang.Iso639_1, input, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(lang.Iso639_2_b, input, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(lang.Iso639_2_t, input, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(lang.Iso639_3, input, StringComparison.OrdinalIgnoreCase))
            ?? MatroskaLanguageOption.Undetermined;
    }
}
