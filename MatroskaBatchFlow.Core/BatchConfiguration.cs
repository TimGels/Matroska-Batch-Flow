using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core
{
    /// <summary>
    /// Represents the configuration for batch processing of media files.
    /// </summary>
    public class BatchConfiguration
    {
        public string DirectoryPath { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public IList<TrackConfiguration> AudioTracks { get; set; } = [];
        public IList<TrackConfiguration> VideoTracks { get; set; } = [];
        public IList<TrackConfiguration> SubtitleTracks { get; set; } = [];
    }

    /// <summary>
    /// Represents the configuration for a specific media track.
    /// </summary>
    public sealed class TrackConfiguration
    {
        public TrackType TrackType { get; set; }
        public int Position { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public bool Default { get; set; }
        public bool Forced { get; set; }
        public bool Remove { get; set; }
    }
}
