using MatroskaBatchFlow.Core.Enums;
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

        /// <summary>
        /// Returns the list of <see cref="TrackConfiguration"/> objects for the specified <see cref="TrackType"/>.
        /// </summary>
        /// <param name="type">The track type.</param>
        /// <returns>
        /// The corresponding list of <see cref="TrackConfiguration"/> objects for the given track type.
        /// If the track type is not <see cref="TrackType.Audio"/>, <see cref="TrackType.Video"/>, or <see cref="TrackType.Text"/>, it returns an empty list.
        /// </returns>
        public IList<TrackConfiguration> GetTrackListForType(TrackType type);
    }
}
