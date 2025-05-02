using MKVBatchFlow.Core.Enums;

namespace MKVBatchFlow.Core
{
    /// <summary>
    /// Represents the configuration options for processing a media file.
    /// </summary>
    public class BatchConfiguration
    {
        /// <summary>
        /// The path of the directory to process.
        /// </summary>
        public string DirectoryPath { get; set; } = string.Empty;

        /// <summary>
        /// The title of the files.
        /// </summary>
        public string Title { get; set; } = string.Empty;


        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// List of audio track configurations.
        /// </summary>
        public IList<TrackConfiguration> AudioTracks { get; set; } = [];

        /// <summary>
        /// List of video track configurations.
        /// </summary>
        public IList<TrackConfiguration> VideoTracks { get; set; } = [];

        /// <summary>
        /// List of subtitle track configurations.
        /// </summary>
        public IList<TrackConfiguration> SubtitleTracks { get; set; } = [];

        /// <summary>
        /// Represents the configuration options for a specific track.
        /// </summary>
        public sealed class TrackConfiguration // Changed from private to internal
        {
            public TrackType TrackType { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Language { get; set; } = string.Empty;
            public bool Default { get; set; }
            public bool Forced { get; set; }
            public bool Remove { get; set; }
        }
    }
}
