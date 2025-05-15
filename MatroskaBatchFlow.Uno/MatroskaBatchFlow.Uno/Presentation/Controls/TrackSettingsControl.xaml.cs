using System.Diagnostics;
using System.Globalization;
using MatroskaBatchFlow.Core;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Uno.Presentation.Controls;

public sealed partial class TrackSettingsControl : UserControl
{
    private TrackConfiguration? _currentTrack;

    private bool _isUpdatingFromModel = false;

    public static readonly DependencyProperty TracksProperty =
        DependencyProperty.Register(
            "Tracks",
            typeof(IList<TrackConfiguration>),
            typeof(TrackSettingsControl),
            new PropertyMetadata(default));

    public static readonly DependencyProperty SelectedTrackProperty =
        DependencyProperty.Register(
            nameof(SelectedTrack),
            typeof(TrackConfiguration),
            typeof(TrackSettingsControl),
            new PropertyMetadata(null, OnSelectedTrackChanged));

    public static readonly DependencyProperty IsDefaultTrackProperty =
        DependencyProperty.Register(
            "IsDefaultTrack",
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(false, OnIsTrackSettingChanged));

    public static readonly DependencyProperty IsForcedTrackProperty =
        DependencyProperty.Register(
            "IsForcedTrack",
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(false, OnIsTrackSettingChanged));

    public static readonly DependencyProperty IsRemoveTrackProperty =
        DependencyProperty.Register(
            "IsRemoveTrack",
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(false, OnIsTrackSettingChanged));

    public static readonly DependencyProperty ChangeDefaultTrackProperty =
        DependencyProperty.Register(
            "ChangeDefaultTrack",
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ChangeForcedTrackProperty =
        DependencyProperty.Register(
            "ChangeForcedTrack",
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ChangeEnabledTrackProperty =
        DependencyProperty.Register(
            "ChangeEnabledTrack",
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty TrackNameProperty =
        DependencyProperty.Register(
            "TrackName",
            typeof(string),
            typeof(TrackSettingsControl),
            new PropertyMetadata(string.Empty, OnIsTrackSettingChanged));

    public static readonly DependencyProperty ChangeTrackNameProperty =
        DependencyProperty.Register(
            "ChangeTrackName",
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(true));

    public static readonly DependencyProperty AvailableLanguagesProperty =
    DependencyProperty.Register(
        "AvailableLanguages",
        typeof(ImmutableList<MatroskaLanguageOption>),
        typeof(TrackSettingsControl),
        new PropertyMetadata(default));

    // In your ViewModel or service class
    // public IReadOnlyList<string> AvailableLanguages { get; } = CultureInfo
    //.GetCultures(CultureTypes.AllCultures)
    //.Where(c => !string.IsNullOrEmpty(c.Name))
    //.Select(c => c.Name.ToLowerInvariant()) // BCP47 recommends lowercase  
    //.Distinct()
    //.OrderBy(l => l)
    //.ToList();

    public IList<TrackConfiguration> Tracks
    {
        get => (IList<TrackConfiguration>)GetValue(TracksProperty);
        set => SetValue(TracksProperty, value);
    }

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

    public bool IsRemoveTrack
    {
        get => (bool)GetValue(IsRemoveTrackProperty);
        set => SetValue(IsRemoveTrackProperty, value);
    }

    public bool ChangeDefaultTrack
    {
        get => (bool)GetValue(ChangeDefaultTrackProperty);
        set => SetValue(ChangeDefaultTrackProperty, value);
    }

    public bool ChangeForcedTrack
    {
        get => (bool)GetValue(ChangeForcedTrackProperty);
        set => SetValue(ChangeForcedTrackProperty, value);
    }

    public bool ChangeEnabledTrack
    {
        get => (bool)GetValue(ChangeEnabledTrackProperty);
        set => SetValue(ChangeEnabledTrackProperty, value);
    }

    public string TrackName
    {
        get => (string)GetValue(TrackNameProperty);
        set => SetValue(TrackNameProperty, value);
    }

    public bool ChangeTrackName
    {
        get => (bool)GetValue(ChangeTrackNameProperty);
        set => SetValue(ChangeTrackNameProperty, value);
    }

    public TrackConfiguration? SelectedTrack
    {
        get => (TrackConfiguration?)GetValue(SelectedTrackProperty);
        set => SetValue(SelectedTrackProperty, value);
    }

    public ImmutableList<MatroskaLanguageOption> AvailableLanguages
    {
        get => (ImmutableList<MatroskaLanguageOption>)GetValue(AvailableLanguagesProperty);
        set => SetValue(AvailableLanguagesProperty, value);
    }

    public IList<MatroskaLanguageOption> FilteredLanguages
    {
        get; set;
    }

    public TrackSettingsControl()
    {
        this.InitializeComponent();
        var x = new CultureInfo("Und");
    }

    private void OnLanguageTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            FilteredLanguages = AvailableLanguages
                .Where(lang => lang.Name.Contains(sender.Text, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    /// <summary>
    /// Called when the selected track changes.
    /// </summary>
    /// <param name="dependencyObject"></param>
    /// <param name="e"></param>
    private static void OnSelectedTrackChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        var control = (TrackSettingsControl)dependencyObject;
        control._currentTrack = e.NewValue as TrackConfiguration;
        control.LoadTrackProperties();
    }

    /// <summary>
    /// Loads the track properties from the model to the UI.
    /// </summary>
    private void LoadTrackProperties()
    {
        // Flag to indicate that the UI is being updated from the model.
        // Prevents feedback loops or unintended updates during data synchronization.
        _isUpdatingFromModel = true;

        if (_currentTrack == null)
        {
            IsDefaultTrack = false;
            IsForcedTrack = false;
            IsRemoveTrack = false;
            TrackName = string.Empty;
            return;
        }

        // Load from track to UI
        IsDefaultTrack = _currentTrack.Default;
        IsForcedTrack = _currentTrack.Forced;
        IsRemoveTrack = _currentTrack.Remove;
        TrackName = _currentTrack.Name;

        _isUpdatingFromModel = false;
    }

    /// <summary>
    /// Updates the track properties from the UI to the model.
    /// </summary>
    private void UpdateTrackFromUI()
    {
        if (_isUpdatingFromModel || _currentTrack == null)
            return;

        // Only update if the "Change" flag is true
        if (ChangeDefaultTrack) _currentTrack.Default = IsDefaultTrack;
        if (ChangeForcedTrack)
        {
            _currentTrack.Forced = IsForcedTrack;
            Debug.WriteLine($"Forced setting Track {_currentTrack.Position} updated to value {_currentTrack.Forced}");
        }
        if (ChangeEnabledTrack) _currentTrack.Remove = !IsRemoveTrack;
        if (ChangeTrackName) _currentTrack.Name = TrackName;

        Tracks[_currentTrack.Position - 1] = _currentTrack;
    }

    /// <summary>
    /// Property changed handlers
    /// </summary>
    /// <param name="d"></param>
    /// <param name="e"></param>
    private static void OnIsTrackSettingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (TrackSettingsControl)d;
        control.UpdateTrackFromUI();
    }

    // Handle text change and present suitable items
    private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        // Since selecting an item will also change the text,
        // only listen to changes caused by user entering text.
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            var suitableItems = new List<string>();
            var splitText = sender.Text.ToLower().Split(" ");
            foreach (var cat in AvailableLanguages)
            {
                var found = splitText.All((key) =>
                {
                    return cat.Name.ToLower().Contains(key);
                });
                if (found)
                {
                    suitableItems.Add(cat.Name);
                }
            }
            if (suitableItems.Count == 0)
            {
                suitableItems.Add("No results found");
            }
            sender.ItemsSource = suitableItems;
        }
    }

}
