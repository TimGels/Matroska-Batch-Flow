using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Services;

namespace MatroskaBatchFlow.Uno.Presentation;
public partial class SubtitleViewModel : TrackViewModelBase
{
    public ObservableCollection<TrackConfiguration> SubtitleTracks
    {
        get => _tracks;
        set
        {
            if (_tracks != value)
            {
                _tracks = value;
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
        MutateNameTrack = new RelayCommand(() => _batchConfiguration.Title = DateTime.Now.ToString());
        SubtitleTracks = [.. _batchConfiguration.SubtitleTracks];
    }

    /// <inheritdoc />
    protected override IList<TrackConfiguration> GetTracks() => SubtitleTracks;

    /// <inheritdoc />
    /// <remarks>This method subscribes to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the 
    /// batch configuration and the <see cref="INotifyCollectionChanged.CollectionChanged"/> event of the subtitle 
    /// tracks collection.</remarks>
    protected override void SetupEventHandlers()
    {
        _batchConfiguration.PropertyChanged += OnBatchConfigurationChanged;
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

    /// <inheritdoc />
    /// <remarks>
    /// This method specifically listens for changes to the <c>SubtitleTracks</c> property of the batch
    /// configuration. If the <c>SubtitleTracks</c> property changes, the <see cref="SubtitleTracks"/> property is updated
    /// accordingly.
    /// </remarks>
    /// <param name="sender">The source of the event. This parameter is typically the batch configuration object.</param>
    /// <param name="eventArgs">The event data containing the name of the property that changed. The <see
    /// cref="PropertyChangedEventArgs.PropertyName"/> must not be <see langword="null"/>.</param>
    protected override void OnBatchConfigurationChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is null)
            return;
        // Replace SubtitleTracks only if corresponding property changed.
        if (!nameof(_batchConfiguration.SubtitleTracks).Equals(eventArgs.PropertyName))
            return;

        SubtitleTracks = [.. _batchConfiguration.SubtitleTracks];
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
