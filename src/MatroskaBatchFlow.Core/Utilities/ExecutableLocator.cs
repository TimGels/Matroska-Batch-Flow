namespace MatroskaBatchFlow.Core.Utilities;

/// <summary>
/// Utility class to locate executable binaries in various directories.
/// </summary>
public static class ExecutableLocator
{
    /// <summary>
    /// Finds the specified executable binary in /Binaries, the specified path, the application base directory, or in the system PATH.
    /// </summary>
    /// <param name="binaryName">The name of the executable binary to find.</param>
    /// <returns>The full path to the executable if found, otherwise <see langword="null"/>.</returns>
    public static string? FindExecutable(string binaryName)
    {
        if (Path.IsPathRooted(binaryName))
            return FindRootedExecutable(binaryName);

        string baseDir = AppContext.BaseDirectory;

        // "/Binaries" directory.
        string binariesDir = Path.Combine(baseDir, "Binaries");
        string? found = FindInDirectory(binariesDir, binaryName);
        if (found is not null)
            return found;

        // Application base directory.
        found = FindInDirectory(baseDir, binaryName);
        if (found is not null)
            return found;

        // Current working directory.
        found = FindInDirectory(Environment.CurrentDirectory, binaryName);
        if (found is not null)
            return found;

        // System PATH.
        found = FindInPath(binaryName);
        if (found is not null)
            return found;

        return null;
    }

    /// <summary>
    /// Finds the specified executable binary in a rooted path.
    /// </summary>
    /// <param name="binaryName">The name of the executable binary to find.</param>
    /// <returns>The full path to the executable if found, otherwise <see langword="null"/>.</returns>
    private static string? FindRootedExecutable(string binaryName)
    {
        if (File.Exists(binaryName))
            return binaryName;

        if (OperatingSystem.IsWindows() && !binaryName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            string exeWithExt = binaryName + ".exe";
            if (File.Exists(exeWithExt))
                return exeWithExt;
        }

        return null;
    }

    /// <summary>
    /// Finds the specified executable binary in a given directory.
    /// </summary>
    /// <param name="directory">The directory to search in.</param>
    /// <param name="binaryName">The name of the executable binary to find.</param>
    /// <returns>The full path to the executable if found, otherwise <see langword="null"/>.</returns>
    private static string? FindInDirectory(string directory, string binaryName)
    {
        string candidatePath = Path.Combine(directory, binaryName);

        if (File.Exists(candidatePath))
            return candidatePath;

        if (OperatingSystem.IsWindows() && !binaryName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            string candidateWithExt = candidatePath + ".exe";
            if (File.Exists(candidateWithExt))
                return candidateWithExt;
        }

        return null;
    }

    /// <summary>
    /// Finds the specified executable in the system PATH.
    /// </summary>
    /// <param name="binaryName">The name of the executable binary to find.</param>
    /// <returns>The full path to the executable if found, otherwise <see langword="null"/>.</returns>
    private static string? FindInPath(string binaryName)
    {
        string[] paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator);

        foreach (string path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
                continue;

            string? found = FindInDirectory(path, binaryName);
            if (found is not null)
                return found;
        }

        return null;
    }
}
