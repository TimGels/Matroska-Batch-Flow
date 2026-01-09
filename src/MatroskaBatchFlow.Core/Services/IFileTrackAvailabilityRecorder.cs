using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Records track availability information for scanned files.
/// </summary>
/// <remarks>
/// This service is responsible for tracking how many tracks of each type
/// exist in each file, enabling validation and processing rules to make
/// decisions based on track availability.
/// </remarks>
public interface IFileTrackAvailabilityRecorder
{
    /// <summary>
    /// Records the track availability for a scanned file.
    /// </summary>
    /// <param name="scannedFile">The scanned file to record availability for.</param>
    /// <returns>The recorded track availability information.</returns>
    FileTrackAvailability RecordAvailability(ScannedFileInfo scannedFile);
}
