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

    /// <summary>
    /// Gets a value indicating whether the subtitle page is enabled based on whether files have been added to the program.
    /// </summary>
    public bool IsFileListPopulated => _batchConfiguration.FileList.Count > 0;

    public SubtitleViewModel(ILanguageProvider languageProvider, IBatchConfiguration batchConfiguration)
        : base(languageProvider, batchConfiguration)
    {
        SubtitleTracks = [.. _batchConfiguration.SubtitleTracks];

        SelectedTrack = SubtitleTracks.FirstOrDefault();

        SetupEventHandlers();
    }

    /// <inheritdoc />
    protected override IList<TrackConfiguration> GetTracks() => SubtitleTracks;

    /// <inheritdoc />
    /// <remarks>This method subscribes to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of the 
    /// batch configuration and the <see cref="INotifyCollectionChanged.CollectionChanged"/> event of the subtitle 
    /// tracks collection.</remarks>
    protected sealed override void SetupEventHandlers()
    {
        _batchConfiguration.PropertyChanged += OnBatchConfigurationChanged;
        _batchConfiguration.SubtitleTracks.CollectionChanged += OnBatchConfigurationSubtitleTracksChanged;

        // Subscribe to FileList changes to update IsFileListPopulated property.
        _batchConfiguration.FileList.CollectionChanged += OnFileListChanged;
    }

    /// <summary>
    /// Handles changes to the FileList collection to update the IsFileListPopulated property.
    /// </summary>
    /// <param name="sender">The source of the event, typically the FileList collection.</param>
    /// <param name="eventArgs">The event data containing information about the changes to the collection.</param>
    private void OnFileListChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        OnPropertyChanged(nameof(IsFileListPopulated));
    }

    /// <summary>
    /// Handles changes to the collection of subtitle track configurations in a batch configuration.
    /// </summary>
    /// <remarks>This method responds to changes in the subtitle track configurations by subscribing to or
    /// unsubscribing from  property change notifications for the affected items. It also handles the <see
    /// cref="NotifyCollectionChangedAction.Reset"/>  action, ensuring that all items in the collection are properly
    /// updated when the collection is replaced or cleared.</remarks>
    /// <param name="sender">The source of the event, typically the collection that was changed.</param>
    /// <param name="eventArgs">The event data containing information about the changes to the collection, such as added, removed, or replaced
    /// items.</param>
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

        // Handle the Reset action, which indicates that the entire collection has been replaced or cleared.
        if (eventArgs.Action == NotifyCollectionChangedAction.Reset)
        {
            var all = _batchConfiguration.SubtitleTracks;
            Unsubscribe(all);
            Subscribe(all);
        }

        SubtitleTracks = [.. _batchConfiguration.SubtitleTracks];
        SelectedTrack = SubtitleTracks.FirstOrDefault();
    }

    /// <inheritdoc />
    /// <remarks>
    /// This method listens for changes to the <c>SubtitleTracks</c> property in the batch configuration
    /// and updates the <see cref="SubtitleTracks"/> property accordingly. If the <c>PropertyName</c> in <paramref
    /// name="eventArgs"/> is <c>null</c> or does not match the <c>SubtitleTracks</c> property, the method exits without
    /// making changes.
    /// </remarks>
    /// <param name="sender">The source of the event. This parameter is not used in the method.</param>
    /// <param name="eventArgs">The event data containing the name of the changed property. The <see
    /// cref="PropertyChangedEventArgs.PropertyName"/> must not be <see langword="null"/>.</param>
    protected override void OnBatchConfigurationChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is null)
            return;

        // Replace SubtitleTracks only if the corresponding property changed.
        if (!nameof(_batchConfiguration.SubtitleTracks).Equals(eventArgs.PropertyName))
            return;

        SubtitleTracks = [.. _batchConfiguration.SubtitleTracks];
    }
}
