using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Uno.Presentation;
public partial class SubtitleViewModel : TrackViewModelBase
{
    private ObservableCollection<TrackConfiguration> _subtitleTracks = [];
    public ObservableCollection<TrackConfiguration> SubtitleTracks
    {
        get => _subtitleTracks;
        set
        {
            if (_subtitleTracks != value)
            {
                _subtitleTracks = value;
                OnPropertyChanged(nameof(SubtitleTracks));
                OnPropertyChanged(nameof(IsTrackSelected));
            }
        }
    }

    public ICommand ClearSubtitleTracks { get; }
    public ICommand MutateNameTrack { get; }

    public SubtitleViewModel(ILanguageProvider languageProvider, IBatchConfiguration batchConfiguration)
        : base(languageProvider, batchConfiguration)
    {
        ClearSubtitleTracks = new RelayCommand(ClearSubtitleTracksAction);
        MutateNameTrack = new RelayCommand(() => _batchConfiguration.SubtitleTracks[0].Name = DateTime.Now.ToString());
        SubtitleTracks = [.. _batchConfiguration.SubtitleTracks];
    }

    /// <inheritdoc />
    protected override IList<TrackConfiguration> GetTracks() => SubtitleTracks;

    /// <summary>
    /// Sets up event handlers to synchronize the state between the <see cref="IBatchConfiguration"/> and the <see cref="SubtitleTracks"/> collection.
    /// </summary>
    /// <remarks>This method ensures that changes to the <see cref="SubtitleTracks"/> collection in the ViewModel
    /// are reflected in the underlying batch configuration, and vice versa. It subscribes to property change
    /// notifications and collection change events to maintain synchronization. Additionally, it handles scenarios such
    /// as adding, removing, or resetting items in the collections.</remarks>
    protected override void SetupEventHandlers()
    {
        _batchConfiguration.PropertyChanged += OnBatchConfigurationChanged;

        SubtitleTracks.CollectionChanged += (s, e) =>
        {
            _batchConfiguration.SubtitleTracks = SubtitleTracks;
        };

        _batchConfiguration.SubtitleTracks.CollectionChanged += OnBatchConfigurationSubtitleTracksChanged;
    }

    /// <summary>
    /// Handles changes to the subtitle tracks in the batch configuration.
    /// </summary>
    /// <remarks>This method updates the internal state to reflect changes in the subtitle tracks collection. 
    /// It subscribes to or unsubscribes from property change notifications for the affected tracks, ensuring that
    /// changes to individual track properties are tracked. If the collection is reset, all existing subscriptions are
    /// cleared and re-established for the new collection.</remarks>
    /// <param name="sender">The source of the event, typically the collection that was changed.</param>
    /// <param name="eventArgs">The event data containing details about the collection change, such as the action 
    /// performed and the items affected.</param>
    private void OnBatchConfigurationSubtitleTracksChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        void Subscribe(IEnumerable<TrackConfiguration>? items) =>
            items?.ToList().ForEach(t => t.PropertyChanged += OnTrackPropertyChanged);

        void Unsubscribe(IEnumerable<TrackConfiguration>? items) =>
            items?.ToList().ForEach(t => t.PropertyChanged -= OnTrackPropertyChanged);

        if (eventArgs.NewItems != null)
            Subscribe(eventArgs.NewItems.Cast<TrackConfiguration>());
        if (eventArgs.OldItems != null)
            Unsubscribe(eventArgs.OldItems.Cast<TrackConfiguration>());

        if (eventArgs.Action == NotifyCollectionChangedAction.Reset)
        {
            var all = _batchConfiguration.SubtitleTracks;
            Unsubscribe(all);
            Subscribe(all);
        }

        SubtitleTracks = [.. _batchConfiguration.SubtitleTracks];
    }

    /// <summary>
    /// Handles changes to the batch configuration and updates the relevant subtitle track properties.
    /// </summary>
    /// <remarks>This method updates the <see cref="SubtitleTracks"/> collection or synchronizes the
    /// properties of the currently selected track with the updated track in the batch configuration, depending on the
    /// property that changed. If the property name is <see langword="null"/>, the method exits without making
    /// changes.</remarks>
    /// <param name="sender">The source of the event. This parameter is optional and can be <see langword="null"/>.</param>
    /// <param name="eventArgs">The event data containing the name of the property that changed.</param>
    private void OnBatchConfigurationChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is null)
            return;

        if (nameof(SubtitleTracks).Equals(eventArgs.PropertyName))
        {
            SubtitleTracks = [.. _batchConfiguration.SubtitleTracks];
        } else if (SelectedTrack != null && SelectedTrack.Position > 0 && SelectedTrack.Position <= _batchConfiguration.SubtitleTracks.Count)
        {
            var updatedTrack = _batchConfiguration.SubtitleTracks[SelectedTrack.Position - 1];
            PropertyInfo? propInfo = typeof(TrackConfiguration).GetProperty(eventArgs.PropertyName);
            if (propInfo != null && propInfo.CanRead && propInfo.CanWrite)
            {
                var value = propInfo.GetValue(updatedTrack);
                propInfo.SetValue(SelectedTrack, value);
            }
        }
    }

    /// <summary>
    /// Clears all subtitle tracks in the batch configuration.
    /// </summary>
    /// <remarks>Used during development only.</remarks>
    private void ClearSubtitleTracksAction()
    {
        _batchConfiguration.SubtitleTracks.Clear();
    }
}
