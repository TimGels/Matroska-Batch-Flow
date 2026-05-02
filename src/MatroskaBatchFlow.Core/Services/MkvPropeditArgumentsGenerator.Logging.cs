using MatroskaBatchFlow.Core.Enums;
using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// LoggerMessage definitions for <see cref="MkvPropeditArgumentsGenerator"/>.
/// </summary>
public sealed partial class MkvPropeditArgumentsGenerator
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Generated mkvpropedit arguments for {CommandCount} of {FileCount} file(s).")]
    private partial void LogBatchArgumentsGenerated(int fileCount, int commandCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping {TrackType} track index {TrackIndex} for file '{FilePath}' because the track does not exist.")]
    private partial void LogTrackMissingInFile(string filePath, TrackType trackType, int trackIndex);
}
