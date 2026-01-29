using MatroskaBatchFlow.Core.Logging;
using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// LoggerMessage definitions for <see cref="LanguageProvider"/>.
/// </summary>
public partial class LanguageProvider
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Loaded {LanguageCount} languages from {FilePath}")]
    private partial void LogLanguagesLoaded(int languageCount, string filePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Language file not found at {FilePath}, using empty language list")]
    private partial void LogLanguageFileNotFound(string filePath);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to load language data from {FilePath}")]
    private partial void LogLanguageLoadFailed(Exception ex, string filePath);
}
