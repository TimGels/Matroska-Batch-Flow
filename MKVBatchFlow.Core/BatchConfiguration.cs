using MKVBatchFlow.Core.Enums;

namespace MKVBatchFlow.Core
{
    /// <summary>
    /// Represents the configuration options for processing a media file.
    /// </summary>
    internal class BatchConfiguration
    {
        /// <summary>
        /// The file path of the media file to process.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// General options for processing the file.
        /// </summary>
        public bool OverwriteExisting { get; set; } = false;

        /// <summary>
        /// List of audio track configurations.
        /// </summary>
        public IReadOnlyList<TrackConfiguration> AudioTracks => _audioTracks.AsReadOnly();

        /// <summary>
        /// List of video track configurations.
        /// </summary>
        public IReadOnlyList<TrackConfiguration> VideoTracks => _videoTracks.AsReadOnly();

        /// <summary>
        /// List of subtitle track configurations.
        /// </summary>
        public IReadOnlyList<TrackConfiguration> SubtitleTracks => _subtitleTracks.AsReadOnly();

        private readonly List<TrackConfiguration> _audioTracks = [];
        private readonly List<TrackConfiguration> _videoTracks = [];
        private readonly List<TrackConfiguration> _subtitleTracks = [];

        /// <summary>
        /// Adds a new audio track configuration.
        /// </summary>
        /// <param name="trackType">The type of the track (e.g., "Audio").</param>
        /// <param name="language">The language of the track.</param>
        /// <param name="isDefault">Whether the track is the default track.</param>
        /// <param name="isForced">Whether the track is forced.</param>
        /// <param name="remove">Whether the track should be removed.</param>
        public void AddAudioTrack(string language, bool isDefault, bool isForced, bool remove)
        {
            _audioTracks.Add(new TrackConfiguration(TrackType.Audio, language, isDefault, isForced, remove));
        }

        /// <summary>
        /// Adds a new video track configuration.
        /// </summary>
        /// <param name="trackType">The type of the track (e.g., "Video").</param>
        /// <param name="language">The language of the track.</param>
        /// <param name="isDefault">Whether the track is the default track.</param>
        /// <param name="isForced">Whether the track is forced.</param>
        /// <param name="remove">Whether the track should be removed.</param>
        public void AddVideoTrack(string language, bool isDefault, bool isForced, bool remove)
        {
            _videoTracks.Add(new TrackConfiguration(TrackType.Video, language, isDefault, isForced, remove));
        }

        /// <summary>
        /// Adds a new subtitle track configuration.
        /// </summary>
        /// <param name="trackType"></param>
        /// <param name="language"></param>
        /// <param name="isDefault"></param>
        /// <param name="isForced"></param>
        /// <param name="remove"></param>
        public void AddSubtitleTrack(string language, bool isDefault, bool isForced, bool remove)
        {
            _subtitleTracks.Add(new TrackConfiguration(TrackType.Subtitle, language, isDefault, isForced, remove));
        }

        /// <summary>
        /// Represents the configuration options for a specific track.
        /// </summary>
        internal sealed class TrackConfiguration // Changed from private to internal
        {
            public TrackType TrackType { get; set; }
            public string Language { get; set; }
            public bool Default { get; set; }
            public bool Forced { get; set; }
            public bool Remove { get; set; }

            internal TrackConfiguration(TrackType trackType, string language, bool isDefault, bool isForced, bool remove)
            {
                TrackType = trackType;
                Language = language;
                Default = isDefault;
                Forced = isForced;
                Remove = remove;
            }
        }
    }
}
