namespace MatroskaBatchFlow.Uno.Presentation;

/// <summary>
/// LoggerMessage definitions for <see cref="TrackViewModelBase"/>.
/// </summary>
public abstract partial class TrackViewModelBase
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "SelectedLanguage was set to null; falling back to Undetermined. This is unexpected during normal UI operation.")]
    private partial void LogSelectedLanguageNullFallback();
}
