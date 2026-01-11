namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// Implementation of <see cref="IPlatformService"/> that provides platform-specific information.
/// </summary>
public sealed class PlatformService : IPlatformService
{
    /// <inheritdoc/>
    public bool IsWindows() => OperatingSystem.IsWindows();
}
