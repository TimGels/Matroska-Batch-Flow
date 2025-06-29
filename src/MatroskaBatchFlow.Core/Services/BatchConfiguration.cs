using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using static MatroskaBatchFlow.Core.MediaInfoResult.MediaInfo;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Represents the configuration for batch processing of media files.
/// </summary>
public class BatchConfiguration : INotifyPropertyChanged, IBatchConfiguration
{
    private string _directoryPath = string.Empty;
    private string _title = string.Empty;
    private readonly ObservableCollection<ScannedFileInfo> _fileList = [];
    private ObservableCollection<TrackConfiguration> _audioTracks = [];
    private ObservableCollection<TrackConfiguration> _videoTracks = [];
    private ObservableCollection<TrackConfiguration> _subtitleTracks = [];
    private static readonly ImmutableList<TrackConfiguration> _emptyTrackList = [];

    public event PropertyChangedEventHandler? PropertyChanged;

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
}

/// <summary>
/// Represents the configuration for a specific media track. Properties that are null should be seen as 
/// missing from the scanned Matroska file.
/// </summary>
/// <remarks>Constructor parameter <paramref name="trackInfo"/> is needed to due generation error when using object initializer syntax.</remarks>
/// <param name="trackInfo">The <see cref="TrackInfo"/> instance containing the scanned track information. Must not be null.</param>
public sealed class TrackConfiguration(TrackInfo trackInfo) : INotifyPropertyChanged
{
    private TrackType _trackType;

    private int _position;
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

    public TrackType TrackType
    {
        get => _trackType;
        set
        {
            if (_trackType != value)
            {
                _trackType = value;
                OnPropertyChanged(nameof(TrackType));
            }
        }
    }

    public int Position
    {
        get => _position;
        set
        {
            if (_position != value)
            {
                _position = value;
                OnPropertyChanged(nameof(Position));
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
