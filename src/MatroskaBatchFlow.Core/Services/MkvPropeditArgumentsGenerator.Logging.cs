using MatroskaBatchFlow.Core.Enums;
using Microsoft.Extensions.Logging;

namespace MatroskaBatchFlow.Core.Services;

/// <summary>
/// LoggerMessage definitions for <see cref="MkvPropeditArgumentsGenerator"/>.
/// </summary>
public sealed partial class MkvPropeditArgumentsGenerator
{
    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Per-file {TrackType} track index {TrackIndex} exceeds global track count {GlobalTrackCount} for file: {FilePath}")]
    private partial void LogPerFileTrackExceedsGlobalCount(string filePath, TrackType trackType, int trackIndex, int globalTrackCount);
}
