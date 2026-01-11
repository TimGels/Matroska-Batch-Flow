namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Provides platform-specific information for the current operating system.
/// </summary>
public interface IPlatformService
{
    /// <summary>
    /// Gets a value indicating whether the current operating system is Windows.
    /// </summary>
    bool IsWindows();
}
