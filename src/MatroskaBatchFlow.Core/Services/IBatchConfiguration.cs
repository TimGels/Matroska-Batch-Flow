using System.Collections.ObjectModel;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

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
    string MkvpropeditArguments { get; set; }

    ObservableCollection<ScannedFileInfo> FileList { get; }
    ObservableCollection<TrackConfiguration> AudioTracks { get; set; }
    ObservableCollection<TrackConfiguration> VideoTracks { get; set; }
    ObservableCollection<TrackConfiguration> SubtitleTracks { get; set; }

    /// <summary>
    /// Clears the configuration settings.
    /// </summary>
    public void Clear();

    /// <summary>
    /// Returns the list of <see cref="TrackConfiguration"/> objects for the specified <see cref="TrackType"/>.
    /// </summary>
    /// <param name="trackType">The track type.</param>
    /// <returns>
    /// The corresponding list of <see cref="TrackConfiguration"/> objects for the given track type.
    /// If the track type is not <see cref="TrackType.Audio"/>, <see cref="TrackType.Video"/>, or <see cref="TrackType.Text"/>, it returns an empty list.
    /// </returns>
    public IList<TrackConfiguration> GetTrackListForType(TrackType trackType);
}
