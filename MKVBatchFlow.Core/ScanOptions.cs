namespace MKVBatchFlow.Core
{
    /// <summary>
    /// Options for scanning files in a directory.
    /// </summary>
    public class ScanOptions
    {
        /// <summary>
        /// The directory path to scan.
        /// </summary>
        public string DirectoryPath { get; set; } = string.Empty;

        /// <summary>
        /// The allowed file extensions for the scan.
        /// </summary>
        public string[] AllowedExtensions { get; set; } = [];

        /// <summary>
        /// Indicating whether the scan should include subdirectories.
        /// </summary>
        public bool Recursive { get; set; }

        /// <summary>
        /// Indicating whether hidden files and directories should be excluded from the scan.
        /// </summary>
        public bool ExcludeHidden { get; set; }
    }
}
