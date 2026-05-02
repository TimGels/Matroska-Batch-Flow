using System.Collections.ObjectModel;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Utilities;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Defines the contract for batch configuration of media files.
/// <br />
/// Track intents in the global collections implement INotifyPropertyChanged,
/// so property changes within tracks can be observed.
/// Per-file values are computed on demand via the transform pipeline at resolution time.
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
    ObservableCollection<TrackIntent> AudioTracks { get; set; }
    ObservableCollection<TrackIntent> VideoTracks { get; set; }
    ObservableCollection<TrackIntent> SubtitleTracks { get; set; }

    event EventHandler? StateChanged;

    /// <summary>
    /// Clears the configuration settings.
    /// </summary>
    public void Clear();

    /// <summary>
    /// Returns the list of <see cref="TrackIntent"/> objects for the specified <see cref="TrackType"/>.
    /// </summary>
    /// <param name="trackType">The track type.</param>
    /// <returns>
    /// The corresponding list of <see cref="TrackIntent"/> objects for the given track type.
    /// If the track type is not <see cref="TrackType.Audio"/>, <see cref="TrackType.Video"/>, or <see cref="TrackType.Text"/>, it returns an empty list.
    /// </returns>
    public IList<TrackIntent> GetTrackListForType(TrackType trackType);

    /// <summary>
    /// Marks a file's metadata as stale (needs re-scanning).
    /// </summary>
    /// <param name="fileId">The unique identifier of the file to mark as stale.</param>
    public void MarkFileAsStale(Guid fileId);

    /// <summary>
    /// Checks if a file's metadata is stale.
    /// </summary>
    /// <param name="fileId">The unique identifier of the file to check.</param>
    /// <returns><see langword="true"/> if the file is stale; otherwise, <see langword="false"/>.</returns>
    public bool IsFileStale(Guid fileId);

    /// <summary>
    /// Clears the stale flag for a file after re-scanning.
    /// </summary>
    /// <param name="fileId">The unique identifier of the file to clear the stale flag for.</param>
    public void ClearStaleFlag(Guid fileId);

    /// <summary>
    /// Gets all files that have stale metadata.
    /// </summary>
    /// <returns>An enumerable collection of files with stale metadata.</returns>
    public IEnumerable<ScannedFileInfo> GetStaleFiles();
}
