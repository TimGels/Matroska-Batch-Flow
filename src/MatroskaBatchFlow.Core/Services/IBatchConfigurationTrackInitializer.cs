using MatroskaBatchFlow.Core.Enums;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Provides functionality to initialize the track configuration for a batch process.
/// </summary>
/// <remarks>This interface defines a method to ensure that the track configuration in a batch process is properly
/// initialized and meets the required track criteria based on the provided reference file and track type by mutating the batch configuration.</remarks>
public interface IBatchConfigurationTrackInitializer
{
    /// <summary>
    /// Ensures that the track count in the batch configuration matches the track count in a reference file for specified track types.
    /// </summary>
    /// <param name="referenceFile">The reference file to validate. Cannot be null.</param>
    /// <param name="trackTypes">The track types to ensure in the batch configuration. If no track types are specified, no action is taken.</param>
    void EnsureTrackCount(ScannedFileInfo referenceFile, params TrackType[] trackTypes);
}
