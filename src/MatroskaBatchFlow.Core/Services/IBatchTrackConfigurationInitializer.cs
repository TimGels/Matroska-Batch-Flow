using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Provides functionality to initialize the track configuration for files in a batch process.
/// </summary>
/// <remarks>
/// This interface defines a method to initialize track intents for scanned files,
/// expanding global track collections to reflect maximum track counts across all files.
/// </remarks>
public interface IBatchTrackConfigurationInitializer
{
    /// <summary>
    /// Initializes track intents for a scanned file based on its detected tracks.
    /// </summary>
    /// <remarks>
    /// <para>This method expands global track collections to reflect maximum track counts across all files.
    /// Per-file values are computed on demand via the transform pipeline at resolution time.</para>
    /// </remarks>
    /// <param name="scannedFile">The scanned file to initialize configurations for.</param>
    /// <param name="trackTypes">The track types to initialize. If empty, no action is taken.</param>
    void Initialize(ScannedFileInfo scannedFile, params TrackType[] trackTypes);
}
