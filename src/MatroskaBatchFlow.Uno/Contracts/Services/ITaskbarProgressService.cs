namespace MatroskaBatchFlow.Uno.Contracts.Services;

/// <summary>
/// Shows the progress of a long-running operation on the application's taskbar button.
/// </summary>
/// <remarks>
/// Supported on Windows, for both the WinAppSDK and the Skia Desktop targets. On other platforms all members do nothing.
/// Members must be called on the UI thread.
/// </remarks>
public interface ITaskbarProgressService
{
    /// <summary>
    /// Shows the specified progress.
    /// </summary>
    /// <param name="fraction">The progress between 0 and 1.</param>
    void SetProgress(double fraction);

    /// <summary>
    /// Shows the progress as paused, for example while an operation is being cancelled.
    /// </summary>
    void SetPaused();

    /// <summary>
    /// Removes the progress from the taskbar button.
    /// </summary>
    void Clear();
}
