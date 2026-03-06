using System.Collections.ObjectModel;
using System.ComponentModel;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;
using MatroskaBatchFlow.Core.Utilities;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Defines the contract for batch configuration of media files.
/// <br />
/// Note: The TrackConfiguration items in the global collections implement INotifyPropertyChanged,
/// so property changes within global tracks can be observed.
/// Per-file track values are stored as <see cref="FileTrackValues"/> which carry no modification intent.
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
    /// Gets the per-file track values for a specific file and track type.
    /// These carry only scanned values (Name, Language, flags) — no modification intent.
    /// Modification flags (<c>ShouldModify*</c>) live on the global <see cref="TrackConfiguration"/> objects returned by <see cref="GetTrackListForType"/>.
    /// </summary>
    /// <param name="fileId">The ScannedFileInfo.Id (Guid) of the file.</param>
    /// <param name="trackType">Type of track to retrieve.</param>
    /// <returns>List of per-file track values for the specified file and track type.</returns>
    public IList<FileTrackValues> GetTrackListForFile(Guid fileId, TrackType trackType);

    /// <summary>
    /// Migrates file configuration from an old file ID to a new file ID.
    /// Used when replacing a file with a re-scanned version that has a new Guid.
    /// Preserves user's configuration while updating the file's metadata.
    /// </summary>
    /// <param name="oldFileId">The original file's unique identifier.</param>
    /// <param name="newFileId">The new file's unique identifier.</param>
    public void MigrateFileConfiguration(Guid oldFileId, Guid newFileId);

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
