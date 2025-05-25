namespace MatroskaBatchFlow.Core
{
    /// <summary>
    /// Represents the information of a scanned media file including its file path.
    /// </summary>
    public sealed record ScannedFileInfo()
    {
        /// <summary>
        /// Path of the scanned file.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Resulting details from the MediaInfo scan.
        /// </summary>
        public MediaInfoResult Result { get; set; }

    }
}
