using MatroskaBatchFlow.Uno.Contracts.Services;
using MatroskaBatchFlow.Uno.Services.Interop;

namespace MatroskaBatchFlow.Uno.Services;

/// <summary>
/// Shows progress on the taskbar button of the main window through the Windows shell (ITaskbarList3).
/// </summary>
/// <remarks>
/// Works for the WinAppSDK target and for the Skia Desktop target when running on Windows. Elsewhere, or when the shell
/// object cannot be created, all calls do nothing. Updates are only sent when the whole percentage changes.
/// </remarks>
public sealed partial class TaskbarProgressService(ILogger<TaskbarProgressService> logger) : ITaskbarProgressService
{
    private const ulong ProgressMaximum = 100;

    private ITaskbarList3? _taskbarList;
    private bool _isUnavailable = !OperatingSystem.IsWindows();
    private bool _isShowingProgress;
    private int _lastPercent = -1;

    /// <inheritdoc />
    public void SetProgress(double fraction)
    {
        var percent = (int)Math.Round(Math.Clamp(fraction, 0d, 1d) * ProgressMaximum);
        if (percent == _lastPercent)
            return;

        // Setting a value switches the button from "no progress" to normal progress, but keeps a paused state.
        if (Apply((taskbarList, hwnd) => taskbarList.SetProgressValue(hwnd, (ulong)percent, ProgressMaximum)))
        {
            _lastPercent = percent;
            _isShowingProgress = true;
        }
    }

    /// <inheritdoc />
    public void SetPaused()
    {
        if (Apply((taskbarList, hwnd) => taskbarList.SetProgressState(hwnd, TaskbarProgressState.Paused)))
        {
            _isShowingProgress = true;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        if (!_isShowingProgress)
            return;

        Apply((taskbarList, hwnd) => taskbarList.SetProgressState(hwnd, TaskbarProgressState.NoProgress));
        _isShowingProgress = false;
        _lastPercent = -1;
    }

    /// <summary>
    /// Runs an action against the taskbar list for the main window.
    /// </summary>
    /// <param name="action">The action to run.</param>
    /// <returns><see langword="true"/> if the action ran; otherwise, <see langword="false"/>.</returns>
    private bool Apply(Action<ITaskbarList3, nint> action)
    {
        if (_isUnavailable)
            return false;

        var hwnd = GetMainWindowHandle();
        if (hwnd == 0)
            return false;

        try
        {
            _taskbarList ??= TaskbarListInterop.Create();
            action(_taskbarList, hwnd);
            return true;
        }
        catch (Exception ex)
        {
            // Taskbar progress is cosmetic; stop trying after the first failure.
            _isUnavailable = true;
            LogTaskbarProgressUnavailable(ex);
            return false;
        }
    }

    /// <summary>
    /// Gets the native window handle (HWND) of the main window.
    /// </summary>
    /// <returns>The window handle, or 0 when the window is not available or is not a Win32 window.</returns>
    private static nint GetMainWindowHandle()
    {
        if (App.MainWindow is not { } window)
            return 0;

#if WINDOWS10_0_19041_0_OR_GREATER
        return WinRT.Interop.WindowNative.GetWindowHandle(window);
#else
        return global::Uno.UI.Xaml.WindowHelper.GetNativeWindow(window) is global::Uno.UI.NativeElementHosting.Win32NativeWindow nativeWindow
            ? nativeWindow.Hwnd
            : 0;
#endif
    }
}
