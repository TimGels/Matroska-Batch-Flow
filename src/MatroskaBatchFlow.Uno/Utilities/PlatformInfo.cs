namespace MatroskaBatchFlow.Uno.Utilities;

public static class PlatformInfo
{
    /// <summary>
    /// Indicates whether the application is running on the Skia rendering backend.
    /// </summary>
    public static bool IsSkia
    {
#if __UNO_SKIA__
        get { return true; }
#else
        get { return false; }
#endif
    }

    /// <summary>
    /// Indicates whether the application is running with the Windows App SDK (WinAppSDK) APIs available.
    /// </summary>
    public static bool IsWinAppSDK
    {
#if WINDOWS10_0_18362_0_OR_GREATER
        get { return true; }
#else
        get { return false; }
#endif
    }
}
