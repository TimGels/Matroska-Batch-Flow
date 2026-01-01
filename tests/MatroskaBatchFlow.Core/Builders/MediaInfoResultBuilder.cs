using System.Diagnostics.CodeAnalysis;
using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Models;

namespace MatroskaBatchFlow.Core.Tests.Builders;

/// <summary>
/// A builder class for constructing MediaInfoResult objects with a fluent API.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Builder class for test data, not production code.")]
public class MediaInfoResultBuilder
{
    private MediaInfoResult.CreatingLibraryInfo _creatingLibrary;
    private MediaInfoResult.MediaInfo _media;
    private int _nextStreamKindId = 0;
    private readonly HashSet<int> _usedStreamKindIds = [];

    /// <summary>
    /// Initializes a new instance of the MediaInfoResultBuilder class with default values.
    /// </summary>
    public MediaInfoResultBuilder()
    {
        // Initialize with default values
        _creatingLibrary = new MediaInfoResult.CreatingLibraryInfo(string.Empty, string.Empty, string.Empty);
        _media = new MediaInfoResult.MediaInfo(string.Empty, []);
    }

    /// <summary>
    /// Sets the CreatingLibrary information.
    /// </summary>
    /// <param name="libraryName">The name of the library.</param>
    /// <param name="version">The version of the library.</param>
    /// <param name="url">The URL of the library.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public MediaInfoResultBuilder WithCreatingLibrary(string libraryName = "", string version = "", string url = "")
    {
        _creatingLibrary = new MediaInfoResult.CreatingLibraryInfo(libraryName, version, url);
        return this;
    }

    /// <summary>
    /// Sets the library name.
    /// </summary>
    /// <param name="name">The name of the library.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public MediaInfoResultBuilder WithLibraryName(string name)
    {
        _creatingLibrary = new MediaInfoResult.CreatingLibraryInfo(
            name,
            _creatingLibrary.Version,
            _creatingLibrary.Url);
        return this;
    }

    /// <summary>
    /// Sets the library version.
    /// </summary>
    /// <param name="version">The version of the library.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public MediaInfoResultBuilder WithLibraryVersion(string version)
    {
        _creatingLibrary = new MediaInfoResult.CreatingLibraryInfo(
            _creatingLibrary.Name,
            version,
            _creatingLibrary.Url);
        return this;
    }

    /// <summary>
    /// Sets the library URL.
    /// </summary>
    /// <param name="url">The URL of the library.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public MediaInfoResultBuilder WithLibraryUrl(string url)
    {
        _creatingLibrary = new MediaInfoResult.CreatingLibraryInfo(
            _creatingLibrary.Name,
            _creatingLibrary.Version,
            url);
        return this;
    }

    /// <summary>
    /// Sets the media reference ID.
    /// </summary>
    /// <param name="reference">The media reference ID.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public MediaInfoResultBuilder WithMediaReference(string reference)
    {
        _media = new MediaInfoResult.MediaInfo(reference, _media.Track);
        return this;
    }

    /// <summary>
    /// Adds a track to the media.
    /// </summary>
    /// <param name="track">The track to add.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public MediaInfoResultBuilder AddTrack(MediaInfoResult.MediaInfo.TrackInfo track)
    {
        var tracks = new List<MediaInfoResult.MediaInfo.TrackInfo>(_media.Track) { track };
        _media = new MediaInfoResult.MediaInfo(_media.Ref, tracks);
        return this;
    }

    /// <summary>
    /// Adds a track of the specified type using TrackInfoBuilder.
    /// If streamKindId is not provided, assigns the next available unique ID.
    /// </summary>
    /// <param name="type">The type of the track (e.g., audio, video, subtitle).</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public MediaInfoResultBuilder AddTrackOfType(TrackType type)
    {
        // Find the next available unique streamKindId
        while (_usedStreamKindIds.Contains(_nextStreamKindId))
            _nextStreamKindId++;
        return AddTrackOfType(type, _nextStreamKindId++);
    }

    /// <summary>
    /// Adds a new track of the specified type and associates it with the given stream kind identifier.
    /// </summary>
    /// <remarks>This method ensures that each track is uniquely associated with a stream kind identifier. Attempting
    /// to reuse an existing <paramref name="streamKindId"/> will result in an exception.</remarks>
    /// <param name="type">The type of the track to add. This determines the track's media category.</param>
    /// <param name="streamKindId">The unique identifier for the stream kind to associate with the track.  Must not already be in use.</param>
    /// <returns>The current <see cref="MediaInfoResultBuilder"/> instance, allowing for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="streamKindId"/> is already in use.</exception>
    public MediaInfoResultBuilder AddTrackOfType(TrackType type, int streamKindId)
    {
        if (_usedStreamKindIds.Contains(streamKindId))
            throw new ArgumentException($"streamKindId {streamKindId} is already used.", nameof(streamKindId));
        _usedStreamKindIds.Add(streamKindId);
        var track = new TrackInfoBuilder()
            .WithType(type)
            .WithStreamKindID(streamKindId)
            .Build();
        return AddTrack(track);
    }

    /// <summary>
    /// Builds and returns a MediaInfoResult object with the configured values.
    /// </summary>
    /// <returns>A new MediaInfoResult instance.</returns>
    public MediaInfoResult Build()
    {
        return new MediaInfoResult(_creatingLibrary, _media);
    }
}

/// <summary>
/// A builder class for constructing TrackInfo objects with a fluent API.
/// </summary>
public class TrackInfoBuilder
{
    private TrackType _type = TrackType.General;
    private string _typeOrder = string.Empty;
    private string _count = string.Empty;
    private string _streamCount = string.Empty;
    private string _streamKind = string.Empty;
    private string _streamKindString = string.Empty;
    private int _streamKindID = 0;
    private string _streamKindPos = string.Empty;
    private string _streamOrder = string.Empty;
    private string _id = string.Empty;
    private string _idString = string.Empty;
    private string _uniqueID = string.Empty;
    private string _format = string.Empty;
    private string _formatString = string.Empty;
    private string _formatInfo = string.Empty;
    private string _formatUrl = string.Empty;
    private string _formatCommercial = string.Empty;
    private string _formatCommercialIfAny = string.Empty;
    private string _formatProfile = string.Empty;
    private string _formatLevel = string.Empty;
    private string _formatTier = string.Empty;
    private string _internetMediaType = string.Empty;
    private string _codecID = string.Empty;
    private string _codecIDInfo = string.Empty;
    private string _codecIDUrl = string.Empty;
    private string _duration = string.Empty;
    private string _durationString = string.Empty;
    private string _durationString1 = string.Empty;
    private string _durationString2 = string.Empty;
    private string _durationString3 = string.Empty;
    private string _durationString4 = string.Empty;
    private string _durationString5 = string.Empty;
    private string _bitRateMode = string.Empty;
    private string _bitRateModeString = string.Empty;
    private string _bitRate = string.Empty;
    private string _bitRateString = string.Empty;
    private string _channels = string.Empty;
    private string _channelsString = string.Empty;
    private string _channelPositions = string.Empty;
    private string _channelPositionsString2 = string.Empty;
    private string _channelLayout = string.Empty;
    private string _samplesPerFrame = string.Empty;
    private string _samplingRate = string.Empty;
    private string _samplingRateString = string.Empty;
    private string _samplingCount = string.Empty;
    private string _frameRate = string.Empty;
    private string _frameRateString = string.Empty;
    private string _frameCount = string.Empty;
    private string _compressionMode = string.Empty;
    private string _compressionModeString = string.Empty;
    private string _delay = string.Empty;
    private string _delayString = string.Empty;
    private string _streamSize = string.Empty;
    private string _streamSizeString = string.Empty;
    private string _streamSizeProportion = string.Empty;
    private string _title = string.Empty;
    private string _language = string.Empty;
    private string _languageString = string.Empty;
    private string _languageString1 = string.Empty;
    private string _languageString2 = string.Empty;
    private string _languageString3 = string.Empty;
    private string _languageString4 = string.Empty;
    private string _serviceKind = string.Empty;
    private string _serviceKindString = string.Empty;
    private bool _default = false;
    private string _defaultString = string.Empty;
    private bool _forced = false;
    private string _forcedString = string.Empty;
    private MediaInfoResult.MediaInfo.TrackInfo.ExtraInfo _extra =
        new(string.Empty, string.Empty, string.Empty, string.Empty,
            string.Empty, string.Empty, string.Empty, string.Empty,
            string.Empty, string.Empty, string.Empty, []);

    /// <summary>
    /// Sets the track type.
    /// </summary>
    /// <param name="type">The track type.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithType(TrackType type)
    {
        _type = type;
        return this;
    }

    /// <summary>
    /// Sets the type order.
    /// </summary>
    /// <param name="typeOrder">The type order.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithTypeOrder(string typeOrder)
    {
        _typeOrder = typeOrder;
        return this;
    }

    /// <summary>
    /// Sets the count.
    /// </summary>
    /// <param name="count">The count.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithCount(string count)
    {
        _count = count;
        return this;
    }

    /// <summary>
    /// Sets the stream count.
    /// </summary>
    /// <param name="streamCount">The stream count.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithStreamCount(string streamCount)
    {
        _streamCount = streamCount;
        return this;
    }

    /// <summary>
    /// Sets the stream kind.
    /// </summary>
    /// <param name="streamKind">The stream kind.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithStreamKind(string streamKind)
    {
        _streamKind = streamKind;
        return this;
    }

    /// <summary>
    /// Sets the stream kind string.
    /// </summary>
    /// <param name="streamKindString">The stream kind string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithStreamKindString(string streamKindString)
    {
        _streamKindString = streamKindString;
        return this;
    }

    /// <summary>
    /// Sets the stream kind ID.
    /// </summary>
    /// <param name="streamKindID">The stream kind ID.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithStreamKindID(int streamKindID)
    {
        _streamKindID = streamKindID;
        return this;
    }

    /// <summary>
    /// Sets the stream kind position.
    /// </summary>
    /// <param name="streamKindPos">The stream kind position.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithStreamKindPos(string streamKindPos)
    {
        _streamKindPos = streamKindPos;
        return this;
    }

    /// <summary>
    /// Sets the stream order.
    /// </summary>
    /// <param name="streamOrder">The stream order.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithStreamOrder(string streamOrder)
    {
        _streamOrder = streamOrder;
        return this;
    }

    /// <summary>
    /// Sets the ID.
    /// </summary>
    /// <param name="id">The ID.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithID(string id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Sets the ID string.
    /// </summary>
    /// <param name="idString">The ID string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithIDString(string idString)
    {
        _idString = idString;
        return this;
    }

    /// <summary>
    /// Sets the unique ID.
    /// </summary>
    /// <param name="uniqueID">The unique ID.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithUniqueID(string uniqueID)
    {
        _uniqueID = uniqueID;
        return this;
    }

    /// <summary>
    /// Sets the format.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithFormat(string format)
    {
        _format = format;
        return this;
    }

    /// <summary>
    /// Sets the format string.
    /// </summary>
    /// <param name="formatString">The format string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithFormatString(string formatString)
    {
        _formatString = formatString;
        return this;
    }

    /// <summary>
    /// Sets the format info.
    /// </summary>
    /// <param name="formatInfo">The format info.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithFormatInfo(string formatInfo)
    {
        _formatInfo = formatInfo;
        return this;
    }

    /// <summary>
    /// Sets the format URL.
    /// </summary>
    /// <param name="formatUrl">The format URL.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithFormatUrl(string formatUrl)
    {
        _formatUrl = formatUrl;
        return this;
    }

    /// <summary>
    /// Sets the format commercial.
    /// </summary>
    /// <param name="formatCommercial">The format commercial.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithFormatCommercial(string formatCommercial)
    {
        _formatCommercial = formatCommercial;
        return this;
    }

    /// <summary>
    /// Sets the format commercial if any.
    /// </summary>
    /// <param name="formatCommercialIfAny">The format commercial if any.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithFormatCommercialIfAny(string formatCommercialIfAny)
    {
        _formatCommercialIfAny = formatCommercialIfAny;
        return this;
    }

    /// <summary>
    /// Sets the format profile.
    /// </summary>
    /// <param name="formatProfile">The format profile.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithFormatProfile(string formatProfile)
    {
        _formatProfile = formatProfile;
        return this;
    }

    /// <summary>
    /// Sets the format level.
    /// </summary>
    /// <param name="formatLevel">The format level.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithFormatLevel(string formatLevel)
    {
        _formatLevel = formatLevel;
        return this;
    }

    /// <summary>
    /// Sets the format tier.
    /// </summary>
    /// <param name="formatTier">The format tier.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithFormatTier(string formatTier)
    {
        _formatTier = formatTier;
        return this;
    }

    /// <summary>
    /// Sets the internet media type.
    /// </summary>
    /// <param name="internetMediaType">The internet media type.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithInternetMediaType(string internetMediaType)
    {
        _internetMediaType = internetMediaType;
        return this;
    }

    /// <summary>
    /// Sets the codec ID.
    /// </summary>
    /// <param name="codecID">The codec ID.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithCodecID(string codecID)
    {
        _codecID = codecID;
        return this;
    }

    /// <summary>
    /// Sets the codec ID info.
    /// </summary>
    /// <param name="codecIDInfo">The codec ID info.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithCodecIDInfo(string codecIDInfo)
    {
        _codecIDInfo = codecIDInfo;
        return this;
    }

    /// <summary>
    /// Sets the codec ID URL.
    /// </summary>
    /// <param name="codecIDUrl">The codec ID URL.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithCodecIDUrl(string codecIDUrl)
    {
        _codecIDUrl = codecIDUrl;
        return this;
    }

    /// <summary>
    /// Sets the duration.
    /// </summary>
    /// <param name="duration">The duration.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithDuration(string duration)
    {
        _duration = duration;
        return this;
    }

    /// <summary>
    /// Sets the duration string.
    /// </summary>
    /// <param name="durationString">The duration string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithDurationString(string durationString)
    {
        _durationString = durationString;
        return this;
    }

    /// <summary>
    /// Sets the duration string 1.
    /// </summary>
    /// <param name="durationString1">The duration string 1.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithDurationString1(string durationString1)
    {
        _durationString1 = durationString1;
        return this;
    }

    /// <summary>
    /// Sets the duration string 2.
    /// </summary>
    /// <param name="durationString2">The duration string 2.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithDurationString2(string durationString2)
    {
        _durationString2 = durationString2;
        return this;
    }

    /// <summary>
    /// Sets the duration string 3.
    /// </summary>
    /// <param name="durationString3">The duration string 3.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithDurationString3(string durationString3)
    {
        _durationString3 = durationString3;
        return this;
    }

    /// <summary>
    /// Sets the duration string 4.
    /// </summary>
    /// <param name="durationString4">The duration string 4.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithDurationString4(string durationString4)
    {
        _durationString4 = durationString4;
        return this;
    }

    /// <summary>
    /// Sets the duration string 5.
    /// </summary>
    /// <param name="durationString5">The duration string 5.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithDurationString5(string durationString5)
    {
        _durationString5 = durationString5;
        return this;
    }

    /// <summary>
    /// Sets the bitrate mode.
    /// </summary>
    /// <param name="bitRateMode">The bitrate mode.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithBitRateMode(string bitRateMode)
    {
        _bitRateMode = bitRateMode;
        return this;
    }

    /// <summary>
    /// Sets the bitrate mode string.
    /// </summary>
    /// <param name="bitRateModeString">The bitrate mode string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithBitRateModeString(string bitRateModeString)
    {
        _bitRateModeString = bitRateModeString;
        return this;
    }

    /// <summary>
    /// Sets the bitrate.
    /// </summary>
    /// <param name="bitRate">The bitrate.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithBitRate(string bitRate)
    {
        _bitRate = bitRate;
        return this;
    }

    /// <summary>
    /// Sets the bitrate string.
    /// </summary>
    /// <param name="bitRateString">The bitrate string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithBitRateString(string bitRateString)
    {
        _bitRateString = bitRateString;
        return this;
    }

    /// <summary>
    /// Sets the channels.
    /// </summary>
    /// <param name="channels">The channels.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithChannels(string channels)
    {
        _channels = channels;
        return this;
    }

    /// <summary>
    /// Sets the channels string.
    /// </summary>
    /// <param name="channelsString">The channels string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithChannelsString(string channelsString)
    {
        _channelsString = channelsString;
        return this;
    }

    /// <summary>
    /// Sets the channel positions.
    /// </summary>
    /// <param name="channelPositions">The channel positions.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithChannelPositions(string channelPositions)
    {
        _channelPositions = channelPositions;
        return this;
    }

    /// <summary>
    /// Sets the channel positions string 2.
    /// </summary>
    /// <param name="channelPositionsString2">The channel positions string 2.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithChannelPositionsString2(string channelPositionsString2)
    {
        _channelPositionsString2 = channelPositionsString2;
        return this;
    }

    /// <summary>
    /// Sets the channel layout.
    /// </summary>
    /// <param name="channelLayout">The channel layout.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithChannelLayout(string channelLayout)
    {
        _channelLayout = channelLayout;
        return this;
    }

    /// <summary>
    /// Sets the samples per frame.
    /// </summary>
    /// <param name="samplesPerFrame">The samples per frame.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithSamplesPerFrame(string samplesPerFrame)
    {
        _samplesPerFrame = samplesPerFrame;
        return this;
    }

    /// <summary>
    /// Sets the sampling rate.
    /// </summary>
    /// <param name="samplingRate">The sampling rate.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithSamplingRate(string samplingRate)
    {
        _samplingRate = samplingRate;
        return this;
    }

    /// <summary>
    /// Sets the sampling rate string.
    /// </summary>
    /// <param name="samplingRateString">The sampling rate string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithSamplingRateString(string samplingRateString)
    {
        _samplingRateString = samplingRateString;
        return this;
    }

    /// <summary>
    /// Sets the sampling count.
    /// </summary>
    /// <param name="samplingCount">The sampling count.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithSamplingCount(string samplingCount)
    {
        _samplingCount = samplingCount;
        return this;
    }

    /// <summary>
    /// Sets the frame rate.
    /// </summary>
    /// <param name="frameRate">The frame rate.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithFrameRate(string frameRate)
    {
        _frameRate = frameRate;
        return this;
    }

    /// <summary>
    /// Sets the frame rate string.
    /// </summary>
    /// <param name="frameRateString">The frame rate string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithFrameRateString(string frameRateString)
    {
        _frameRateString = frameRateString;
        return this;
    }

    /// <summary>
    /// Sets the frame count.
    /// </summary>
    /// <param name="frameCount">The frame count.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithFrameCount(string frameCount)
    {
        _frameCount = frameCount;
        return this;
    }

    /// <summary>
    /// Sets the compression mode.
    /// </summary>
    /// <param name="compressionMode">The compression mode.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithCompressionMode(string compressionMode)
    {
        _compressionMode = compressionMode;
        return this;
    }

    /// <summary>
    /// Sets the compression mode string.
    /// </summary>
    /// <param name="compressionModeString">The compression mode string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithCompressionModeString(string compressionModeString)
    {
        _compressionModeString = compressionModeString;
        return this;
    }

    /// <summary>
    /// Sets the delay.
    /// </summary>
    /// <param name="delay">The delay.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithDelay(string delay)
    {
        _delay = delay;
        return this;
    }

    /// <summary>
    /// Sets the delay string.
    /// </summary>
    /// <param name="delayString">The delay string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithDelayString(string delayString)
    {
        _delayString = delayString;
        return this;
    }

    /// <summary>
    /// Sets the stream size.
    /// </summary>
    /// <param name="streamSize">The stream size.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithStreamSize(string streamSize)
    {
        _streamSize = streamSize;
        return this;
    }

    /// <summary>
    /// Sets the stream size string.
    /// </summary>
    /// <param name="streamSizeString">The stream size string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithStreamSizeString(string streamSizeString)
    {
        _streamSizeString = streamSizeString;
        return this;
    }

    /// <summary>
    /// Sets the stream size proportion.
    /// </summary>
    /// <param name="streamSizeProportion">The stream size proportion.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithStreamSizeProportion(string streamSizeProportion)
    {
        _streamSizeProportion = streamSizeProportion;
        return this;
    }

    /// <summary>
    /// Sets the title.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    /// <summary>
    /// Sets the language.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithLanguage(string language)
    {
        _language = language;
        return this;
    }

    /// <summary>
    /// Sets the language string.
    /// </summary>
    /// <param name="languageString">The language string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithLanguageString(string languageString)
    {
        _languageString = languageString;
        return this;
    }

    /// <summary>
    /// Sets the language string 1.
    /// </summary>
    /// <param name="languageString1">The language string 1.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithLanguageString1(string languageString1)
    {
        _languageString1 = languageString1;
        return this;
    }

    /// <summary>
    /// Sets the language string 2.
    /// </summary>
    /// <param name="languageString2">The language string 2.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithLanguageString2(string languageString2)
    {
        _languageString2 = languageString2;
        return this;
    }

    /// <summary>
    /// Sets the language string 3.
    /// </summary>
    /// <param name="languageString3">The language string 3.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithLanguageString3(string languageString3)
    {
        _languageString3 = languageString3;
        return this;
    }

    /// <summary>
    /// Sets the language string 4.
    /// </summary>
    /// <param name="languageString4">The language string 4.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithLanguageString4(string languageString4)
    {
        _languageString4 = languageString4;
        return this;
    }

    /// <summary>
    /// Sets the service kind.
    /// </summary>
    /// <param name="serviceKind">The service kind.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithServiceKind(string serviceKind)
    {
        _serviceKind = serviceKind;
        return this;
    }

    /// <summary>
    /// Sets the service kind string.
    /// </summary>
    /// <param name="serviceKindString">The service kind string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithServiceKindString(string serviceKindString)
    {
        _serviceKindString = serviceKindString;
        return this;
    }

    /// <summary>
    /// Sets the Default flag.
    /// </summary>
    /// <param name="default">The default flag value.</param>
    /// <returns>The current TrackInfoBuilder instance.</returns>
    public TrackInfoBuilder WithDefault(bool @default)
    {
        _default = @default;
        return this;
    }

    /// <summary>
    /// Sets the default string.
    /// </summary>
    /// <param name="defaultString">The default string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithDefaultString(string defaultString)
    {
        _defaultString = defaultString;
        return this;
    }

    /// <summary>
    /// Sets the forced.
    /// </summary>
    /// <param name="forced">The forced.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithForced(bool forced)
    {
        _forced = forced;
        return this;
    }

    /// <summary>
    /// Sets the forced string.
    /// </summary>
    /// <param name="forcedString">The forced string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithForcedString(string forcedString)
    {
        _forcedString = forcedString;
        return this;
    }

    /// <summary>
    /// Sets the extra information.
    /// </summary>
    /// <param name="extra">The extra information.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TrackInfoBuilder WithExtra(MediaInfoResult.MediaInfo.TrackInfo.ExtraInfo extra)
    {
        _extra = extra;
        return this;
    }

    /// <summary>
    /// Builds and returns a TrackInfo object with the configured values.
    /// </summary>
    /// <returns>A new TrackInfo instance.</returns>
    public MediaInfoResult.MediaInfo.TrackInfo Build()
    {
        return new MediaInfoResult.MediaInfo.TrackInfo(
            _type, _typeOrder, _count, _streamCount, _streamKind,
            _streamKindString, _streamKindID, _streamKindPos, _streamOrder,
            _id, _idString, _uniqueID, _format, _formatString,
            _formatInfo, _formatUrl, _formatCommercial, _formatCommercialIfAny,
            _formatProfile, _formatLevel, _formatTier, _internetMediaType,
            _codecID, _codecIDInfo, _codecIDUrl, _duration,
            _durationString, _durationString1, _durationString2, _durationString3,
            _durationString4, _durationString5, _bitRateMode, _bitRateModeString,
            _bitRate, _bitRateString, _channels, _channelsString,
            _channelPositions, _channelPositionsString2, _channelLayout,
            _samplesPerFrame, _samplingRate, _samplingRateString, _samplingCount,
            _frameRate, _frameRateString, _frameCount, _compressionMode,
            _compressionModeString, _delay, _delayString, _streamSize,
            _streamSizeString, _streamSizeProportion, _title, _language,
            _languageString, _languageString1, _languageString2, _languageString3,
            _languageString4, _serviceKind, _serviceKindString, _default,
            _defaultString, _forced, _forcedString, _extra);
    }
}

/// <summary>
/// A builder class for constructing ExtraInfo objects with a fluent API.
/// </summary>
public class ExtraInfoBuilder
{
    private string _attachments = string.Empty;
    private string _distributedBy = string.Empty;
    private string _md5Unencoded = string.Empty;
    private string _bsid = string.Empty;
    private string _dialnorm = string.Empty;
    private string _dialnormString = string.Empty;
    private string _compr = string.Empty;
    private string _comprString = string.Empty;
    private string _dsurmod = string.Empty;
    private string _acmod = string.Empty;
    private string _lfeon = string.Empty;
    private Dictionary<string, string> _chapters = [];

    /// <summary>
    /// Sets the attachments.
    /// </summary>
    /// <param name="attachments">The attachments.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ExtraInfoBuilder WithAttachments(string attachments)
    {
        _attachments = attachments;
        return this;
    }

    /// <summary>
    /// Sets the distributed by.
    /// </summary>
    /// <param name="distributedBy">The distributed by.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ExtraInfoBuilder WithDistributedBy(string distributedBy)
    {
        _distributedBy = distributedBy;
        return this;
    }

    /// <summary>
    /// Sets the MD5 unencoded.
    /// </summary>
    /// <param name="md5Unencoded">The MD5 unencoded.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ExtraInfoBuilder WithMD5Unencoded(string md5Unencoded)
    {
        _md5Unencoded = md5Unencoded;
        return this;
    }

    /// <summary>
    /// Sets the BSID.
    /// </summary>
    /// <param name="bsid">The BSID.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ExtraInfoBuilder WithBsid(string bsid)
    {
        _bsid = bsid;
        return this;
    }

    /// <summary>
    /// Sets the dialnorm.
    /// </summary>
    /// <param name="dialnorm">The dialnorm.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ExtraInfoBuilder WithDialnorm(string dialnorm)
    {
        _dialnorm = dialnorm;
        return this;
    }

    /// <summary>
    /// Sets the dialnorm string.
    /// </summary>
    /// <param name="dialnormString">The dialnorm string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ExtraInfoBuilder WithDialnormString(string dialnormString)
    {
        _dialnormString = dialnormString;
        return this;
    }

    /// <summary>
    /// Sets the compr.
    /// </summary>
    /// <param name="compr">The compr.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ExtraInfoBuilder WithCompr(string compr)
    {
        _compr = compr;
        return this;
    }

    /// <summary>
    /// Sets the compr string.
    /// </summary>
    /// <param name="comprString">The compr string.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ExtraInfoBuilder WithComprString(string comprString)
    {
        _comprString = comprString;
        return this;
    }

    /// <summary>
    /// Sets the dsurmod.
    /// </summary>
    /// <param name="dsurmod">The dsurmod.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ExtraInfoBuilder WithDsurmod(string dsurmod)
    {
        _dsurmod = dsurmod;
        return this;
    }

    /// <summary>
    /// Sets the acmod.
    /// </summary>
    /// <param name="acmod">The acmod.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ExtraInfoBuilder WithAcmod(string acmod)
    {
        _acmod = acmod;
        return this;
    }

    /// <summary>
    /// Sets the lfeon.
    /// </summary>
    /// <param name="lfeon">The lfeon.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ExtraInfoBuilder WithLfeon(string lfeon)
    {
        _lfeon = lfeon;
        return this;
    }

    /// <summary>
    /// Sets the chapters.
    /// </summary>
    /// <param name="chapters">The chapters.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ExtraInfoBuilder WithChapters(Dictionary<string, string> chapters)
    {
        _chapters = chapters ?? [];
        return this;
    }

    /// <summary>
    /// Adds a chapter.
    /// </summary>
    /// <param name="id">The chapter ID.</param>
    /// <param name="name">The chapter name.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ExtraInfoBuilder AddChapter(string id, string name)
    {
        if (id != null)
        {
            _chapters[id] = name;
        }
        return this;
    }

    /// <summary>
    /// Builds and returns an ExtraInfo object with the configured values.
    /// </summary>
    /// <returns>A new ExtraInfo instance.</returns>
    public MediaInfoResult.MediaInfo.TrackInfo.ExtraInfo Build()
    {
        return new MediaInfoResult.MediaInfo.TrackInfo.ExtraInfo(
            _attachments, _distributedBy, _md5Unencoded, _bsid,
            _dialnorm, _dialnormString, _compr, _comprString,
            _dsurmod, _acmod, _lfeon, _chapters);
    }
}
