using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Utilities;
using Microsoft.Extensions.Logging;
using static MatroskaBatchFlow.Core.Models.MediaInfoResult.MediaInfo;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Represents the configuration for batch processing of media files.
/// </summary>
public partial class BatchConfiguration : IBatchConfiguration
{
    private readonly ILogger<BatchConfiguration> _logger;
    private string _directoryPath = string.Empty;
    private bool _shouldModifyTitle = false;
    private string _title = string.Empty;
    private bool _addTrackStatisticsTags = true;
    private bool _deleteTrackStatisticsTags = false;
    private bool _shouldModifyTrackStatisticsTags = false;
    /// <summary>
    /// Maintains a collection of scanned files, ensuring each file is unique by its path within the current batch
    /// configuration.
    /// </summary>
    /// <remarks>This collection uses the <see cref="IScannedFileInfoPathComparer"/> to prevent duplicate entries for the
    /// same physical file, avoiding redundant processing during batch operations.</remarks>
    private readonly UniqueObservableCollection<ScannedFileInfo> _fileList;
    private readonly HashSet<Guid> _staleFileIds = [];
    private ObservableCollection<TrackConfiguration> _audioTracks = [];
    private ObservableCollection<TrackConfiguration> _videoTracks = [];
    private ObservableCollection<TrackConfiguration> _subtitleTracks = [];
    private static readonly ImmutableList<TrackConfiguration> _emptyTrackList = [];
    private string _mkvpropeditArguments = string.Empty;
    private Dictionary<Guid, FileTrackConfiguration> _fileConfigurations = [];

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? StateChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchConfiguration"/> class.
    /// </summary>
    /// <param name="fileComparer">The comparer to use for identifying unique files in the collection.</param>
    /// <param name="logger">The logger instance for logging stale file tracking operations.</param>
    public BatchConfiguration(IScannedFileInfoPathComparer fileComparer, ILogger<BatchConfiguration> logger)
    {
        _logger = logger;
        _fileList = new(fileComparer);

        FileList.CollectionChanged += (sender, e) =>
        {
            // We only care about removals, resets, and replacements for the purpose of clearing state.
            if (e.Action is not NotifyCollectionChangedAction.Remove
            and not NotifyCollectionChangedAction.Reset and not NotifyCollectionChangedAction.Replace)
                return;

            OnFileRemoval(sender, e);

            OnStateChanged();
        };
        AudioTracks.CollectionChanged += (s, e) => TrackCollectionChanged(AudioTracks, e);
        VideoTracks.CollectionChanged += (s, e) => TrackCollectionChanged(VideoTracks, e);
        SubtitleTracks.CollectionChanged += (s, e) => TrackCollectionChanged(SubtitleTracks, e);
    }

    /// <summary>
    /// Handles changes to an <see cref="ObservableCollection{T}"/> of <see cref="TrackConfiguration"/> objects and
    /// updates subscriptions to their <see cref="INotifyPropertyChanged.PropertyChanged"/> events accordingly.
    /// </summary>
    /// <param name="collection">The collection of <see cref="TrackConfiguration"/> objects being tracked.</param>
    /// <param name="eventArgs">The event data describing the changes to the collection.</param>
    private void TrackCollectionChanged(ObservableCollection<TrackConfiguration> collection, NotifyCollectionChangedEventArgs eventArgs)
    {
        void Subscribe(IEnumerable<TrackConfiguration> trackConfigurations)
        {
            foreach (var trackConfiguration in trackConfigurations)
            {
                trackConfiguration.PropertyChanged += TrackConfiguration_PropertyChanged;
            }
        }

        void Unsubscribe(IEnumerable<TrackConfiguration> trackConfigurations)
        {
            foreach (var trackConfiguration in trackConfigurations)
            {
                trackConfiguration.PropertyChanged -= TrackConfiguration_PropertyChanged;
            }
        }

        bool stateChanged = false;

        switch (eventArgs.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (eventArgs.NewItems?.Count > 0)
                {
                    Subscribe(eventArgs.NewItems.Cast<TrackConfiguration>());
                    stateChanged = true;
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                if (eventArgs.OldItems?.Count > 0)
                {
                    Unsubscribe(eventArgs.OldItems.Cast<TrackConfiguration>());
                    stateChanged = true;
                }
                break;
            case NotifyCollectionChangedAction.Replace:
                if (eventArgs.OldItems?.Count > 0)
                {
                    Unsubscribe(eventArgs.OldItems.Cast<TrackConfiguration>());
                }

                if (eventArgs.NewItems?.Count > 0)
                {
                    Subscribe(eventArgs.NewItems.Cast<TrackConfiguration>());
                }

                stateChanged = true;
                break;
            case NotifyCollectionChangedAction.Reset:
                if (collection.Count > 0)
                {
                    Unsubscribe(collection);
                    Subscribe(collection);
                    stateChanged = true;
                }
                break;
        }

        if (stateChanged)
        {
            OnStateChanged();
        }
    }

    private void TrackConfiguration_PropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        OnStateChanged();
    }

    public string DirectoryPath
    {
        get => _directoryPath;
        set
        {
            if (_directoryPath != value)
            {
                _directoryPath = value;
                OnPropertyChanged(nameof(DirectoryPath));
            }
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
    }

    public bool ShouldModifyTitle
    {
        get => _shouldModifyTitle;
        set
        {
            if (_shouldModifyTitle != value)
            {
                _shouldModifyTitle = value;
                OnPropertyChanged(nameof(ShouldModifyTitle));
            }
        }
    }

    public bool AddTrackStatisticsTags
    {
        get => _addTrackStatisticsTags;
        set
        {
            if (_addTrackStatisticsTags != value)
            {
                _addTrackStatisticsTags = value;
                OnPropertyChanged(nameof(AddTrackStatisticsTags));
            }
        }
    }

    public bool DeleteTrackStatisticsTags
    {
        get => _deleteTrackStatisticsTags;
        set
        {
            if (_deleteTrackStatisticsTags != value)
            {
                _deleteTrackStatisticsTags = value;
                OnPropertyChanged(nameof(DeleteTrackStatisticsTags));
            }
        }
    }

    public bool ShouldModifyTrackStatisticsTags
    {
        get => _shouldModifyTrackStatisticsTags;
        set
        {
            if (_shouldModifyTrackStatisticsTags != value)
            {
                _shouldModifyTrackStatisticsTags = value;
                OnPropertyChanged(nameof(ShouldModifyTrackStatisticsTags));
            }
        }
    }


    public UniqueObservableCollection<ScannedFileInfo> FileList => _fileList;

    public ObservableCollection<TrackConfiguration> AudioTracks
    {
        get => _audioTracks;
        set
        {
            if (!ReferenceEquals(_audioTracks, value))
            {
                _audioTracks = value;
                OnPropertyChanged(nameof(AudioTracks));
            }
        }
    }

    public ObservableCollection<TrackConfiguration> VideoTracks
    {
        get => _videoTracks;
        set
        {
            if (!ReferenceEquals(_videoTracks, value))
            {
                _videoTracks = value;
                OnPropertyChanged(nameof(VideoTracks));
            }
        }
    }

    public ObservableCollection<TrackConfiguration> SubtitleTracks
    {
        get => _subtitleTracks;
        set
        {
            if (!ReferenceEquals(_subtitleTracks, value))
            {
                _subtitleTracks = value;
                OnPropertyChanged(nameof(SubtitleTracks));
            }
        }
    }

    public string MkvpropeditArguments
    {
        get => _mkvpropeditArguments;
        set
        {
            if (_mkvpropeditArguments != value)
            {
                _mkvpropeditArguments = value;
                OnPropertyChanged(nameof(MkvpropeditArguments));
            }
        }
    }

    public Dictionary<Guid, FileTrackConfiguration> FileConfigurations
    {
        get => _fileConfigurations;
        set
        {
            if (!ReferenceEquals(_fileConfigurations, value))
            {
                _fileConfigurations = value;
                OnPropertyChanged(nameof(FileConfigurations));
            }
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        OnStateChanged();
    }

    /// <inheritdoc />
    public void Clear()
    {
        DirectoryPath = string.Empty;
        Title = string.Empty;
        FileList.Clear();
        AudioTracks.Clear();
        VideoTracks.Clear();
        SubtitleTracks.Clear();
        FileConfigurations.Clear();
        MkvpropeditArguments = string.Empty;
    }

    /// <inheritdoc />
    public IList<TrackConfiguration> GetTrackListForType(TrackType trackType)
    {
        return trackType switch
        {
            TrackType.Audio => AudioTracks,
            TrackType.Video => VideoTracks,
            TrackType.Text => SubtitleTracks,
            _ => _emptyTrackList
        };
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown if no per-file track configuration exists for the specified file ID.</exception>
    public IList<TrackConfiguration> GetTrackListForFile(Guid fileId, TrackType trackType)
    {
        // Prefer per-file configuration. If none exists, fail fast by throwing an exception.
        if (FileConfigurations.TryGetValue(fileId, out var fileConfig))
        {
            return fileConfig.GetTrackListForType(trackType);
        }

        throw new InvalidOperationException($"No per-file track configuration found for file ID '{fileId}'.");
    }

    /// <summary>
    /// Clears all audio, video, and subtitle tracks from the current collection.
    /// </summary>
    private void ClearTracks()
    {
        AudioTracks.Clear();
        VideoTracks.Clear();
        SubtitleTracks.Clear();
    }

    /// <summary>
    /// Handles the removal of a file and updates the state of the application accordingly.
    /// </summary>
    /// <remarks>This method is triggered when a file is removed from the file list. If the file list becomes
    /// empty, it clears the application state.</remarks>
    /// <param name="sender">The source of the event. This parameter can be <see langword="null"/>.</param>
    /// <param name="eventArgs">The event data associated with the file removal.</param>
    private void OnFileRemoval(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        // Clean up per-file state for removed files.
        // OldItems is populated for Remove/Replace but null for Reset (Clear).
        if (eventArgs.OldItems != null)
        {
            foreach (ScannedFileInfo file in eventArgs.OldItems)
            {
                _staleFileIds.Remove(file.Id); // Clear any stale tracking for the removed file.
                _fileConfigurations.Remove(file.Id); // Remove per-file track configuration for the removed file.
            }
        }

        // If there are still files in the list after the removal,
        // we don't want to clear the state since it may still be relevant to the remaining files.
        if (FileList.Count is not 0)
            return;

        Title = string.Empty;
        DirectoryPath = string.Empty;
        MkvpropeditArguments = string.Empty;
        AddTrackStatisticsTags = true;
        DeleteTrackStatisticsTags = false;
        ShouldModifyTrackStatisticsTags = false;
        ShouldModifyTitle = false;
        FileConfigurations.Clear();
        ClearTracks();
    }

    /// <summary>
    /// Raises the <see cref="StateChanged"/> event to notify subscribers of a state change within this object.
    /// </summary>
    private void OnStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Migrates file configuration from an old file ID to a new file ID.
    /// Used when replacing a file with a re-scanned version that has a new Guid.
    /// Preserves user's configuration while updating the file's metadata.
    /// </summary>
    /// <param name="oldFileId">The original file's unique identifier.</param>
    /// <param name="newFileId">The new file's unique identifier.</param>
    public void MigrateFileConfiguration(Guid oldFileId, Guid newFileId)
    {
        if (_fileConfigurations.TryGetValue(oldFileId, out var config))
        {
            _fileConfigurations.Remove(oldFileId);
            _fileConfigurations[newFileId] = config;
            LogFileConfigurationMigrated(oldFileId, newFileId);
        }
        else
        {
            LogMigrationSkippedNoConfiguration(oldFileId);
        }
    }

    /// <summary>
    /// Marks a file's metadata as stale (needs re-scanning).
    /// </summary>
    /// <param name="fileId">The unique identifier of the file to mark as stale.</param>
    public void MarkFileAsStale(Guid fileId)
    {
        if (!_staleFileIds.Add(fileId))
            return;

        var file = _fileList.FirstOrDefault(f => f.Id == fileId);
        if (file != null)
        {
            LogFileMarkedAsStale(file.Path);
        }
    }

    /// <summary>
    /// Checks if a file's metadata is stale.
    /// </summary>
    /// <param name="fileId">The unique identifier of the file to check.</param>
    /// <returns><see langword="true"/> if the file is stale; otherwise, <see langword="false"/>.</returns>
    public bool IsFileStale(Guid fileId) => _staleFileIds.Contains(fileId);

    /// <summary>
    /// Clears the stale flag for a file after re-scanning.
    /// </summary>
    /// <param name="fileId">The unique identifier of the file to clear the stale flag for.</param>
    public void ClearStaleFlag(Guid fileId)
    {
        if (!_staleFileIds.Remove(fileId))
            return;

        var file = _fileList.FirstOrDefault(f => f.Id == fileId);
        if (file != null)
        {
            LogStaleFlagCleared(file.Path);
        }
    }

    /// <summary>
    /// Gets all files that have stale metadata.
    /// </summary>
    /// <returns>An enumerable collection of files with stale metadata.</returns>
    public IEnumerable<ScannedFileInfo> GetStaleFiles()
        => _fileList.Where(f => _staleFileIds.Contains(f.Id));
}

/// <summary>
/// Represents the configuration for a specific media track. Properties that are null should be seen as 
/// missing from the scanned Matroska file.
/// </summary>
/// <remarks>Constructor parameter <paramref name="trackInfo"/> is needed to due generation error when using object initializer syntax.</remarks>
/// <param name="trackInfo">The <see cref="TrackInfo"/> instance containing the scanned track information. Must not be null.</param>
public sealed class TrackConfiguration(TrackInfo trackInfo) : INotifyPropertyChanged
{
    private TrackType _type;
    private int _index;
    /// <summary>
    /// Represents a human-readable label for a track or segment.
    /// <para>
    /// For <see cref="TrackType.Audio"/>, <see cref="TrackType.Video"/>, and <see cref="TrackType.Text"/>,
    /// this property corresponds to the track's <i>Name</i> element as defined in the Matroska specification.
    /// </para>
    /// <para>
    /// For <see cref="TrackType.General"/>, this property represents the segment's <i>Title</i> element.
    /// </para>
    /// <para>
    /// See the Matroska specification for details:
    /// <list type="bullet">
    ///   <item>
    ///     <i>Name</i> (track): <see href="https://www.matroska.org/technical/elements.html#Name">specification</see>
    ///   </item>
    ///   <item>
    ///     <i>Title</i> (segment): <see href="https://www.matroska.org/technical/elements.html#Title">specification</see>
    ///   </item>
    /// </list>
    /// </para>
    /// </summary>
    private string _name = string.Empty;
    private MatroskaLanguageOption _language = MatroskaLanguageOption.Undetermined;
    private bool _default;
    private bool _forced;
    private bool _remove;
    private bool _shouldModifyDefaultFlag = false;
    private bool _shouldModifyForcedFlag = false;
    private bool _shouldModifyEnabledFlag = false;
    private bool _shouldModifyName = false;
    private bool _shouldModifyLanguage = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    public TrackInfo ScannedTrackInfo { get; init; } = trackInfo ?? throw new ArgumentNullException(nameof(trackInfo));

    public TrackType Type
    {
        get => _type;
        set
        {
            if (_type != value)
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }
    }

    public int Index
    {
        get => _index;
        set
        {
            if (_index != value)
            {
                _index = value;
                OnPropertyChanged(nameof(Index));
            }
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public bool ShouldModifyName
    {
        get => _shouldModifyName;
        set
        {
            if (_shouldModifyName != value)
            {
                _shouldModifyName = value;
                OnPropertyChanged(nameof(ShouldModifyName));
            }
        }
    }

    public MatroskaLanguageOption Language
    {
        get => _language;
        set
        {
            if (_language != value)
            {
                _language = value;
                OnPropertyChanged(nameof(Language));
            }
        }
    }

    public bool ShouldModifyLanguage
    {
        get => _shouldModifyLanguage;
        set
        {
            if (_shouldModifyLanguage != value)
            {
                _shouldModifyLanguage = value;
                OnPropertyChanged(nameof(ShouldModifyLanguage));
            }
        }
    }

    public bool Default
    {
        get => _default;
        set
        {
            if (_default != value)
            {
                _default = value;
                OnPropertyChanged(nameof(Default));
            }
        }
    }

    public bool ShouldModifyDefaultFlag
    {
        get => _shouldModifyDefaultFlag;
        set
        {
            if (_shouldModifyDefaultFlag != value)
            {
                _shouldModifyDefaultFlag = value;
                OnPropertyChanged(nameof(ShouldModifyDefaultFlag));
            }
        }
    }

    public bool Forced
    {
        get => _forced;
        set
        {
            if (_forced != value)
            {
                _forced = value;
                OnPropertyChanged(nameof(Forced));
            }
        }
    }

    public bool ShouldModifyForcedFlag
    {
        get => _shouldModifyForcedFlag;
        set
        {
            if (_shouldModifyForcedFlag != value)
            {
                _shouldModifyForcedFlag = value;
                OnPropertyChanged(nameof(ShouldModifyForcedFlag));
            }
        }
    }

    public bool Enabled
    {
        get => _remove;
        set
        {
            if (_remove != value)
            {
                _remove = value;
                OnPropertyChanged(nameof(Enabled));
            }
        }
    }

    public bool ShouldModifyEnabledFlag
    {
        get => _shouldModifyEnabledFlag;
        set
        {
            if (_shouldModifyEnabledFlag != value)
            {
                _shouldModifyEnabledFlag = value;
                OnPropertyChanged(nameof(ShouldModifyEnabledFlag));
            }
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
