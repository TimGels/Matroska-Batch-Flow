using System.ComponentModel;

namespace MatroskaBatchFlow.Core.Services
{
    /// <summary>
    /// Defines the contract for batch configuration of media files.
    /// <br />
    /// Note: The TrackConfiguration items in the collections also implement INotifyPropertyChanged,
    /// so property changes within tracks can be observed.
    /// </summary>
    public interface IBatchConfiguration : INotifyPropertyChanged
    {
        string DirectoryPath { get; set; }
        string Title { get; set; }
        IList<TrackConfiguration> AudioTracks { get; set; }
        IList<TrackConfiguration> VideoTracks { get; set; }
        IList<TrackConfiguration> SubtitleTracks { get; set; }

        /// <summary>
        /// Clears the configuration settings.
        /// </summary>
        public void Clear();
    }
}
