namespace MKVBatchFlow.Core
{
    /// <summary>
    /// Represents the information of a scanned media file including its file path.
    /// </summary>
    public sealed record ScannedFileInfo()
    {
        /// <summary>
        /// Path of the scanned file.
        /// </summary>
        public required string FilePath { get; init; }

        /// <summary>
        /// Resulting details from the MediaInfo scan.
        /// </summary>
        public required MediaInfoResult Result { get; init; }

    }
}
