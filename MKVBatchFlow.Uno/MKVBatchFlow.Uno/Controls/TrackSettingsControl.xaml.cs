using System.Diagnostics;
using MKVBatchFlow.Core;
using MKVBatchFlow.Core.Enums;

namespace MKVBatchFlow.Uno.Controls;

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

    public static readonly DependencyProperty IsEnabledTrackProperty =
        DependencyProperty.Register(
            "IsEnabledTrack",
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(false, OnIsTrackSettingChanged));

    public static readonly DependencyProperty ChangeDefaultTrackProperty =
        DependencyProperty.Register(
            "ChangeDefaultTrack",
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ChangeForcedTrackProperty =
        DependencyProperty.Register(
            "ChangeForcedTrack",
            typeof(bool),
            typeof(TrackSettingsControl),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ChangeEnabledTrackProperty =
        DependencyProperty.Register(
            "ChangeEnabledTrack",
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

    public bool IsEnabledTrack
    {
        get => (bool)GetValue(IsEnabledTrackProperty);
        set => SetValue(IsEnabledTrackProperty, value);
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

    public TrackSettingsControl()
    {
        this.InitializeComponent();
    }

    public TrackConfiguration? SelectedTrack
    {
        get => (TrackConfiguration?)GetValue(SelectedTrackProperty);
        set => SetValue(SelectedTrackProperty, value);
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
            IsEnabledTrack = false;
            TrackName = string.Empty;
            return;
        }

        // Load from track to UI
        IsDefaultTrack = _currentTrack.Default;
        IsForcedTrack = _currentTrack.Forced;
        IsEnabledTrack = !_currentTrack.Remove;
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
        if (ChangeEnabledTrack) _currentTrack.Remove = !IsEnabledTrack;
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
}
