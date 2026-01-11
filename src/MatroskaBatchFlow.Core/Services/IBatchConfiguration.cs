using System.Collections.ObjectModel;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Utilities;

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
    string MkvpropeditArguments { get; set; }
    string Title { get; set; }
    bool ShouldModifyTitle { get; set; }
    bool AddTrackStatisticsTags { get; set; }
    bool DeleteTrackStatisticsTags { get; set; }
    bool ShouldModifyTrackStatisticsTags { get; set; }

    UniqueObservableCollection<ScannedFileInfo> FileList { get; }
    ObservableCollection<TrackConfiguration> AudioTracks { get; set; }
    ObservableCollection<TrackConfiguration> VideoTracks { get; set; }
    ObservableCollection<TrackConfiguration> SubtitleTracks { get; set; }

    /// <summary>
    /// Per-file track configurations for flexible batch processing.
    /// Key is the ScannedFileInfo.Id (Guid), not the file path.
    /// </summary>
    Dictionary<Guid, FileTrackConfiguration> FileConfigurations { get; set; }

    event EventHandler? StateChanged;

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

    /// <summary>
    /// Gets track configuration for a specific file and track type.
    /// Always uses per-file configurations. Falls back to global if file config not found.
    /// </summary>
    /// <param name="fileId">The ScannedFileInfo.Id (Guid) of the file.</param>
    /// <param name="trackType">Type of track to retrieve.</param>
    /// <returns>List of track configurations for the specified file and track type.</returns>
    public IList<TrackConfiguration> GetTrackListForFile(Guid fileId, TrackType trackType);
}
