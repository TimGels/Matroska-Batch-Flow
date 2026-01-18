namespace MatroskaBatchFlow.Uno.Logging;

/// <summary>
/// Centralized EventId constants for Uno application logging.
/// Ranges:
/// - 8000-8099: App Lifecycle (Activation)
/// - 8100-8199: Navigation
/// - 8500-8599: ViewModels (Batch)
/// - 8600-8699: Error Dialog
/// - 8700-8899: Settings &amp; Persistence
/// </summary>
internal static class UnoLogEvents
{
    /// <summary>
    /// App lifecycle events (8000-8099).
    /// </summary>
    internal static class AppLifecycle
    {
        /// <summary>Application has started with diagnostic information.</summary>
        public const int ApplicationStarted = 8000;

        /// <summary>An unhandled exception occurred in the application.</summary>
        public const int UnhandledExceptionOccurred = 8001;
    }

    /// <summary>
    /// Navigation events (8100-8199).
    /// </summary>
    internal static class Navigation
    {
        /// <summary>Navigating to a page.</summary>
        public const int NavigatingTo = 8100;
    }

    /// <summary>
    /// Batch processing events from MainViewModel (8500-8599).
    /// </summary>
    internal static class Batch
    {
        /// <summary>Batch processing was aborted due to validation or precondition failure.</summary>
        public const int BatchAborted = 8501;

        /// <summary>Unexpected error during batch processing.</summary>
        public const int BatchProcessingError = 8502;
    }

    /// <summary>
    /// Error dialog events (8600-8699).
    /// </summary>
    internal static class ErrorDialog
    {
        /// <summary>Exception details were copied to clipboard.</summary>
        public const int DetailsCopied = 8601;

        /// <summary>Exception log was saved to file.</summary>
        public const int LogSaved = 8602;

        /// <summary>Failed to save exception log file.</summary>
        public const int SaveLogFailed = 8603;
    }

    /// <summary>
    /// Settings persistence events (8700-8799).
    /// </summary>
    internal static class Settings
    {
        /// <summary>Failed to deserialize settings from JSON file.</summary>
        public const int DeserializationFailed = 8701;

        /// <summary>Exception occurred while loading settings.</summary>
        public const int LoadFailed = 8702;

        /// <summary>Failed to save a settings value.</summary>
        public const int SaveFailed = 8703;
    }
}
