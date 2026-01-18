namespace MatroskaBatchFlow.Core.Logging;

/// <summary>
/// Centralized EventId constants for Core library logging.
/// Ranges:
/// - 1000-1199: Configuration (LanguageProvider, etc.)
/// - 1200-1399: File Scanning
/// - 2000-2399: Validation
/// - 4000-4599: Processing Orchestration
/// - 4600-4799: Tool Execution (MkvPropeditExecutor, ProcessRunner)
/// </summary>
internal static class CoreLogEvents
{
    /// <summary>
    /// Configuration-related events (1000-1199).
    /// </summary>
    internal static class Configuration
    {
        /// <summary>Language file not found, using fallback.</summary>
        public const int LanguageFileNotFound = 1001;

        /// <summary>Failed to load language data from file.</summary>
        public const int LanguageLoadFailed = 1002;
    }

    /// <summary>
    /// Validation events (2000-2399).
    /// </summary>
    internal static class Validation
    {
        /// <summary>Validation found an error for a file.</summary>
        public const int ValidationError = 2001;

        /// <summary>Validation found a warning for a file.</summary>
        public const int ValidationWarning = 2002;
    }

    /// <summary>
    /// Processing orchestration events (4000-4599).
    /// </summary>
    internal static class Processing
    {
        /// <summary>mkvpropedit execution failed for a file.</summary>
        public const int MkvpropeditFailed = 4001;

        /// <summary>Unexpected error during file processing.</summary>
        public const int UnexpectedProcessingError = 4002;
    }

    /// <summary>
    /// External tool execution events (4600-4799).
    /// </summary>
    internal static class ToolExecution
    {
        /// <summary>Failed to resolve mkvpropedit executable path.</summary>
        public const int ExecutableResolutionFailed = 4601;

        /// <summary>Exception occurred during mkvpropedit execution.</summary>
        public const int MkvpropeditExecutionException = 4602;

        /// <summary>Failed to start external process.</summary>
        public const int ProcessStartFailed = 4603;
    }
}
