using MatroskaBatchFlow.Core.Enums;
using System.Collections.Immutable;
using System.ComponentModel;

namespace MatroskaBatchFlow.Core.Services
{
    /// <summary>
    /// Represents the configuration for batch processing of media files.
    /// </summary>
    public class BatchConfiguration : INotifyPropertyChanged, IBatchConfiguration
    {
        private string _directoryPath = string.Empty;
        private string _title = string.Empty;
        private IList<TrackConfiguration> _audioTracks = [];
        private IList<TrackConfiguration> _videoTracks = [];
        private IList<TrackConfiguration> _subtitleTracks = [];
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

        public IList<TrackConfiguration> AudioTracks
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

        public IList<TrackConfiguration> VideoTracks
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

        public IList<TrackConfiguration> SubtitleTracks
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
            AudioTracks.Clear();
            VideoTracks = [];
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
    /// Represents the configuration for a specific media track.
    /// </summary>
    public sealed class TrackConfiguration : INotifyPropertyChanged
    {
        private TrackType _trackType;
        private int _position;
        private string _name = string.Empty;
        private string _language = string.Empty;
        private bool _default;
        private bool _forced;
        private bool _remove;

        public event PropertyChangedEventHandler? PropertyChanged;

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

        public string Language
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
}
