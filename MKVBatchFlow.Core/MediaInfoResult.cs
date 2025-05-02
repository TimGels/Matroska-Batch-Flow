using System.Text.Json.Serialization;
using static MKVBatchFlow.Core.MediaInfoResult;
using static MKVBatchFlow.Core.MediaInfoResult.MediaInfo;
using static MKVBatchFlow.Core.MediaInfoResult.MediaInfo.TrackInfo;

namespace MKVBatchFlow.Core
{
    public sealed record MediaInfoResult(

        /// <summary>
        /// Information about the library that created or processed the media file.
        /// </summary>
        CreatingLibraryInfo CreatingLibrary,

        /// <summary>
        /// Detailed media information about the tracks within the media file.
        /// </summary>
        MediaInfo Media)
    {

        /// <summary>
        /// Represents information about the library that processed the media file.
        /// </summary>
        public sealed record CreatingLibraryInfo(
            /// <summary>
            /// The name of the library that processed the file.
            /// </summary>
            string Name,

            /// <summary>
            /// The version of the library used.
            /// </summary>
            string Version,

            /// <summary>
            /// A URL to the library or related resources.
            /// </summary>
            string Url);

        /// <summary>
        /// Represents detailed media information, including the tracks in the media file.
        /// </summary>
        public sealed record MediaInfo(
            /// <summary>
            /// A reference ID for the media.
            /// </summary>
            string Ref,

            /// <summary>
            /// A list of tracks within the media file.
            /// </summary>
            List<TrackInfo> Track)
        {
            /// <summary>
            /// Represents detailed information about a specific track within the media file.
            /// </summary>
            public sealed record TrackInfo(
                [property: JsonPropertyName("@type")]
                /// <summary>
                /// The type of the track (e.g., audio, video, subtitle).
                /// </summary>
                string Type,

                /// <summary>
                /// The order in which the track appears.
                /// </summary>
                string TypeOrder,

                /// <summary>
                /// The count of tracks of this type.
                /// </summary>
                string Count,

                /// <summary>
                /// The stream count for this track.
                /// </summary>
                string StreamCount,

                /// <summary>
                /// The stream kind (e.g., video, audio).
                /// </summary>
                string StreamKind,

                /// <summary>
                /// A string representation of the stream kind.
                /// </summary>
                string StreamKindString,

                /// <summary>
                /// The stream ID for the track.
                /// </summary>
                string StreamKindID,

                /// <summary>
                /// The position of the track stream.
                /// </summary>
                string StreamKindPos,

                /// <summary>
                /// The order of the stream within the media.
                /// </summary>
                string StreamOrder,

                /// <summary>
                /// A unique identifier for the track.
                /// </summary>
                string ID,

                /// <summary>
                /// A string version of the track ID.
                /// </summary>
                string IDString,

                /// <summary>
                /// A globally unique identifier for the track.
                /// </summary>
                string UniqueID,

                /// <summary>
                /// The format of the track (e.g., H.264, AAC).
                /// </summary>
                string Format,

                /// <summary>
                /// A string representation of the format.
                /// </summary>
                string FormatString,

                /// <summary>
                /// Additional information about the format.
                /// </summary>
                string FormatInfo,

                /// <summary>
                /// A URL to the format specification or details.
                /// </summary>
                string FormatUrl,

                /// <summary>
                /// The commercial name for the format.
                /// </summary>
                string FormatCommercial,

                /// <summary>
                /// A commercial version of the format, if applicable.
                /// </summary>
                string FormatCommercialIfAny,

                /// <summary>
                /// The profile of the format.
                /// </summary>
                string FormatProfile,

                /// <summary>
                /// The level of the format.
                /// </summary>
                string FormatLevel,

                /// <summary>
                /// The tier of the format.
                /// </summary>
                string FormatTier,

                /// <summary>
                /// The internet media type for the track.
                /// </summary>
                string InternetMediaType,

                /// <summary>
                /// The codec ID for the track.
                /// </summary>
                string CodecID,

                /// <summary>
                /// Additional information about the codec.
                /// </summary>
                string CodecIDInfo,

                /// <summary>
                /// A URL to the codec specification.
                /// </summary>
                string CodecIDUrl,

                /// <summary>
                /// The duration of the track.
                /// </summary>
                string Duration,

                /// <summary>
                /// A string representation of the track duration.
                /// </summary>
                string DurationString,

                /// <summary>
                /// A variety of duration string formats.
                /// </summary>
                string DurationString1,

                string DurationString2,
                string DurationString3,
                string DurationString4,
                string DurationString5,

                /// <summary>
                /// The bitrate mode of the track.
                /// </summary>
                string BitRateMode,

                /// <summary>
                /// A string representation of the bitrate mode.
                /// </summary>
                string BitRateModeString,

                /// <summary>
                /// The bitrate of the track.
                /// </summary>
                string BitRate,

                /// <summary>
                /// A string representation of the bitrate.
                /// </summary>
                string BitRateString,

                /// <summary>
                /// The number of audio channels for audio tracks.
                /// </summary>
                string Channels,

                /// <summary>
                /// A string representation of the channels.
                /// </summary>
                string ChannelsString,

                /// <summary>
                /// The positions of the audio channels.
                /// </summary>
                string ChannelPositions,

                /// <summary>
                /// A second string representation of channel positions.
                /// </summary>
                string ChannelPositionsString2,

                /// <summary>
                /// The layout of the audio channels.
                /// </summary>
                string ChannelLayout,

                /// <summary>
                /// The number of samples per frame for audio.
                /// </summary>
                string SamplesPerFrame,

                /// <summary>
                /// The sample rate for the track.
                /// </summary>
                string SamplingRate,

                /// <summary>
                /// A string representation of the sample rate.
                /// </summary>
                string SamplingRateString,

                /// <summary>
                /// The sample count of the track.
                /// </summary>
                string SamplingCount,

                /// <summary>
                /// The frame rate for video tracks.
                /// </summary>
                string FrameRate,

                /// <summary>
                /// A string representation of the frame rate.
                /// </summary>
                string FrameRateString,

                /// <summary>
                /// The number of frames in the track.
                /// </summary>
                string FrameCount,

                /// <summary>
                /// The compression mode used for the track.
                /// </summary>
                string CompressionMode,

                /// <summary>
                /// A string representation of the compression mode.
                /// </summary>
                string CompressionModeString,

                /// <summary>
                /// The delay associated with the track.
                /// </summary>
                string Delay,

                /// <summary>
                /// A string representation of the delay.
                /// </summary>
                string DelayString,

                /// <summary>
                /// The stream size of the track.
                /// </summary>
                string StreamSize,

                /// <summary>
                /// A string representation of the stream size.
                /// </summary>
                string StreamSizeString,

                /// <summary>
                /// The stream size as a proportion.
                /// </summary>
                string StreamSizeProportion,

                /// <summary>
                /// The title of the track.
                /// </summary>
                string Title,

                /// <summary>
                /// The language of the track.
                /// </summary>
                string Language,

                /// <summary>
                /// A string representation of the language.
                /// </summary>
                string LanguageString,

                /// <summary>
                /// Additional language representations for the track.
                /// </summary>
                string LanguageString1,
                string LanguageString2,
                string LanguageString3,
                string LanguageString4,

                /// <summary>
                /// The service kind of the track (e.g., default, forced).
                /// </summary>
                string ServiceKind,

                /// <summary>
                /// A string representation of the service kind.
                /// </summary>
                string ServiceKindString,

                /// <summary>
                /// Indicates if the track is the default track.
                /// </summary>
                string Default,

                /// <summary>
                /// A string representation of the default status.
                /// </summary>
                string DefaultString,

                /// <summary>
                /// Indicates if the track is forced (e.g., forced subtitle track).
                /// </summary>
                string Forced,

                /// <summary>
                /// A string representation of the forced status.
                /// </summary>
                string ForcedString,

                /// <summary>
                /// Extra information related to the track, such as compression and normalization settings.
                /// </summary>
                ExtraInfo Extra)
            {
                /// <summary>
                /// Represents additional information about the track, such as compression settings, normalization, and chapters.
                /// </summary>
                public sealed record ExtraInfo(
                    /// <summary>
                    /// Attachments associated with the track.
                    /// </summary>
                    string Attachments,

                    /// <summary>
                    /// The distributor of the media.
                    /// </summary>
                    string DistributedBy,

                    /// <summary>
                    /// The MD5 checksum of the track, unencoded.
                    /// </summary>
                    string MD5Unencoded,

                    /// <summary>
                    /// BSID identifier, typically used for broadcast.
                    /// </summary>
                    string Bsid,

                    /// <summary>
                    /// The dialog normalization value for audio tracks.
                    /// </summary>
                    string Dialnorm,

                    /// <summary>
                    /// A string representation of the dialog normalization.
                    /// </summary>
                    string DialnormString,

                    /// <summary>
                    /// Compression information for the track.
                    /// </summary>
                    string Compr,

                    /// <summary>
                    /// A string representation of the compression.
                    /// </summary>
                    string ComprString,

                    /// <summary>
                    /// Digital surround mode for audio tracks.
                    /// </summary>
                    string Dsurmod,

                    /// <summary>
                    /// Audio configuration code for track.
                    /// </summary>
                    string Acmod,

                    /// <summary>
                    /// Indicates if the track has LFE (Low Frequency Effects).
                    /// </summary>
                    string Lfeon,

                    // Normalization values, average, minimum, maximum
                    // Compression averages and values omitted for brevity...

                    /// <summary>
                    /// A dictionary of chapters for the media, where the key is the chapter identifier and the value is the chapter name.
                    /// </summary>
                    Dictionary<string, string> Chapters);
            }
        }
    }
}
