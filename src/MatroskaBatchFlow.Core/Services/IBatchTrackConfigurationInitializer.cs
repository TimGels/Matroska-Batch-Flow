using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Provides functionality to initialize the track configuration for files in a batch process.
/// </summary>
/// <remarks>
/// This interface defines a method to initialize track configurations for scanned files,
/// creating per-file track configurations and updating global collections for UI display.
/// </remarks>
public interface IBatchTrackConfigurationInitializer
{
    /// <summary>
    /// Initializes track configurations for a scanned file based on its detected tracks.
    /// </summary>
    /// <remarks>
    /// <para>This method:</para>
    /// <list type="bullet">
    /// <item>Records track availability in <see cref="IBatchConfiguration.FileTrackMap"/></item>
    /// <item>Creates file-specific track configurations in <see cref="IBatchConfiguration.FileConfigurations"/></item>
    /// <item>Updates global track collections to reflect maximum track counts across all files</item>
    /// </list>
    /// </remarks>
    /// <param name="scannedFile">The scanned file to initialize configurations for.</param>
    /// <param name="trackTypes">The track types to initialize. If empty, no action is taken.</param>
    void Initialize(ScannedFileInfo scannedFile, params TrackType[] trackTypes);
}
