using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using static MatroskaBatchFlow.Core.Models.MediaInfoResult.MediaInfo;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Represents the configuration for batch processing of media files.
/// </summary>
public class BatchConfiguration : IBatchConfiguration
{
    private string _directoryPath = string.Empty;
    private string _title = string.Empty;
    private readonly ObservableCollection<ScannedFileInfo> _fileList = [];
    private ObservableCollection<TrackConfiguration> _audioTracks = [];
    private ObservableCollection<TrackConfiguration> _videoTracks = [];
    private ObservableCollection<TrackConfiguration> _subtitleTracks = [];
    private static readonly ImmutableList<TrackConfiguration> _emptyTrackList = [];
    private string _mkvpropeditArguments = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public BatchConfiguration()
    {
        FileList.CollectionChanged += (sender, e) =>
        {
            // We only care about removals or resets.
            if (e.Action is not NotifyCollectionChangedAction.Remove
            and not NotifyCollectionChangedAction.Reset)
                return;

            OnFileRemoval(sender, e);
        };
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

    public ObservableCollection<ScannedFileInfo> FileList => _fileList;

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

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
    private void OnFileRemoval(object? sender, EventArgs eventArgs)
    {
        if (FileList.Count is not 0)
            return;

        Title = string.Empty;
        DirectoryPath = string.Empty;
        MkvpropeditArguments = string.Empty;
        ClearTracks();
    }
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

    public bool Remove
    {
        get => _remove;
        set
        {
            if (_remove != value)
            {
                _remove = value;
                OnPropertyChanged(nameof(Remove));
            }
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
