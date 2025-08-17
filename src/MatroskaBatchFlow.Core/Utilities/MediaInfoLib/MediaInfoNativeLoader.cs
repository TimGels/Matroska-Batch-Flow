using System.Reflection;
using System.Runtime.InteropServices;

namespace MatroskaBatchFlow.Core.Utilities.MediaInfoLib;

/// <summary>
/// Provides functionality to ensure the native MediaInfo library is properly loaded for the current platform.
/// </summary>
public static class MediaInfoNativeLoader
{
    public static readonly object EnsureLoaded = Register();

    /// <summary>
    /// Registers a custom DLL import resolver for the <see cref="MediaInfo"/> assembly.
    /// </summary>
    /// <remarks>This method sets a custom resolver to handle the loading of native libraries for the <see
    /// cref="MediaInfo"/> assembly. It ensures that the appropriate native library is resolved and loaded when
    /// required.</remarks>
    /// <returns>Always returns <see langword="null"/>.</returns>
    private static object Register()
    {
        NativeLibrary.SetDllImportResolver(typeof(MediaInfo).Assembly, ResolveMediaInfoLibrary);
        return null!;
    }

    /// <summary>
    /// Resolves and loads the MediaInfo library for the current platform.
    /// </summary>
    /// <param name="libraryName">The name of the library to resolve. This parameter is not used in the current implementation.</param>
    /// <param name="assembly">The assembly from which to resolve the library. This parameter is not used in the current implementation.</param>
    /// <param name="searchPath">An optional search path to use when resolving the library. This parameter is not used in the current
    /// implementation.</param>
    /// <returns>A handle to the loaded library if the library is successfully resolved and loaded; otherwise, <see
    /// cref="IntPtr.Zero"/>.</returns>
    private static IntPtr ResolveMediaInfoLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        string? path = null;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            path = "Binaries/MediaInfo.dll";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            path = "Binaries/libmediainfo.so";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            path = "Binaries/libmediainfo.so";
        }

        if (path is not null && NativeLibrary.TryLoad(path, out var handle))
        {
            return handle;
        }

        return IntPtr.Zero; // fallback to default
    }
}
