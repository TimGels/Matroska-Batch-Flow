using MatroskaBatchFlow.Core.Enums;
using MatroskaBatchFlow.Core.Services;
using Moq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Xunit;

//TODO: should be moved to a separate test project for Core.
namespace MatroskaBatchFlow.Core.Tests.Services
{
    public class BatchConfigurationTrackInitializerTests
    {
        [Fact]
        public void EnsureTrackCount_WhenReferenceFileHasMoreTracks_ShouldAddTracks()
        {
            // Arrange
            var mockConfig = new Mock<IBatchConfiguration>();
            var audioTracks = new List<TrackConfiguration>();
            var videoTracks = new List<TrackConfiguration>();

            mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);
            mockConfig.Setup(c => c.GetTrackListForType(TrackType.Video)).Returns(videoTracks);

            var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

            // Create a reference file with 2 audio tracks and 1 video track
            var referenceFile = CreateTestFile(audioCount: 2, videoCount: 1);

            // Act
            initializer.EnsureTrackCount(referenceFile, TrackType.Audio, TrackType.Video);

            // Assert
            Assert.Equal(2, audioTracks.Count);
            Assert.Equal(1, videoTracks.Count);
            Assert.All(audioTracks, track => Assert.Equal(TrackType.Audio, track.TrackType));
            Assert.All(videoTracks, track => Assert.Equal(TrackType.Video, track.TrackType));
        }

        [Fact]
        public void EnsureTrackCount_WhenReferenceFileHasFewerTracks_ShouldRemoveTracks()
        {
            // Arrange
            var mockConfig = new Mock<IBatchConfiguration>();
            var audioTracks = new List<TrackConfiguration>
            {
                new TrackConfiguration { TrackType = TrackType.Audio },
                new TrackConfiguration { TrackType = TrackType.Audio },
                new TrackConfiguration { TrackType = TrackType.Audio }
            };
            var videoTracks = new List<TrackConfiguration>
            {
                new TrackConfiguration { TrackType = TrackType.Video },
                new TrackConfiguration { TrackType = TrackType.Video }
            };

            mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);
            mockConfig.Setup(c => c.GetTrackListForType(TrackType.Video)).Returns(videoTracks);

            var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

            // Create a reference file with 1 audio track and 0 video tracks
            var referenceFile = CreateTestFile(audioCount: 1, videoCount: 0);

            // Act
            initializer.EnsureTrackCount(referenceFile, TrackType.Audio, TrackType.Video);

            // Assert
            Assert.Single(audioTracks);
            Assert.Empty(videoTracks);
        }

        [Fact]
        public void EnsureTrackCount_WhenTrackCountsMatch_ShouldNotModifyTracks()
        {
            // Arrange
            var mockConfig = new Mock<IBatchConfiguration>();
            var audioTracks = new List<TrackConfiguration>
            {
                new TrackConfiguration { TrackType = TrackType.Audio }
            };
            var videoTracks = new List<TrackConfiguration>
            {
                new TrackConfiguration { TrackType = TrackType.Video },
                new TrackConfiguration { TrackType = TrackType.Video }
            };

            mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);
            mockConfig.Setup(c => c.GetTrackListForType(TrackType.Video)).Returns(videoTracks);

            var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);

            // Create a reference file with matching track counts
            var referenceFile = CreateTestFile(audioCount: 1, videoCount: 2);

            // Act
            initializer.EnsureTrackCount(referenceFile, TrackType.Audio, TrackType.Video);

            // Assert
            Assert.Single(audioTracks);
            Assert.Equal(2, videoTracks.Count);
        }

        [Fact]
        public void EnsureTrackCount_WhenReferenceFileIsNull_ShouldNotModifyTracks()
        {
            // Arrange
            var mockConfig = new Mock<IBatchConfiguration>();
            var audioTracks = new List<TrackConfiguration> { new TrackConfiguration { TrackType = TrackType.Audio } };
            
            mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);
            
            var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);
            
            // Act - Pass null reference file
            initializer.EnsureTrackCount(null, TrackType.Audio);
            
            // Assert - Tracks should remain unchanged
            Assert.Single(audioTracks);
            mockConfig.Verify(c => c.GetTrackListForType(It.IsAny<TrackType>()), Times.Never);
        }

        [Fact]
        public void EnsureTrackCount_WhenNoTrackTypesSpecified_ShouldNotModifyTracks()
        {
            // Arrange
            var mockConfig = new Mock<IBatchConfiguration>();
            var audioTracks = new List<TrackConfiguration> { new TrackConfiguration { TrackType = TrackType.Audio } };
            
            mockConfig.Setup(c => c.GetTrackListForType(TrackType.Audio)).Returns(audioTracks);
            
            var initializer = new BatchConfigurationTrackInitializer(mockConfig.Object);
            var referenceFile = CreateTestFile(audioCount: 2);
            
            // Act - Pass empty track types array
            initializer.EnsureTrackCount(referenceFile, new TrackType[0]);
            
            // Assert - Tracks should remain unchanged
            Assert.Single(audioTracks);
            mockConfig.Verify(c => c.GetTrackListForType(It.IsAny<TrackType>()), Times.Never);
        }

        private ScannedFileInfo CreateTestFile(int audioCount = 0, int videoCount = 0, int subtitleCount = 0)
        {
            var tracks = new List<MediaInfoResult.TrackInfo>();
            
            // Add audio tracks
            for (int i = 0; i < audioCount; i++)
            {
                tracks.Add(new MediaInfoResult.TrackInfo(
                    Type: TrackType.Audio,
                    StreamKindID: i,
                    Language: null,
                    TypeOrder: null, Count: null, StreamCount: null, StreamKind: null,
                    StreamKindString: null, StreamKindPos: null, StreamOrder: null,
                    ID: null, IDString: null, UniqueID: null, Format: null,
                    FormatString: null, FormatInfo: null, FormatUrl: null,
                    FormatCommercial: null, FormatCommercialIfAny: null, FormatProfile: null,
                    FormatLevel: null, FormatTier: null, InternetMediaType: null,
                    CodecID: null, CodecIDInfo: null, CodecIDUrl: null, Duration: null,
                    DurationString: null, DurationString1: null, DurationString2: null,
                    DurationString3: null, DurationString4: null, DurationString5: null,
                    BitRateMode: null, BitRateModeString: null, BitRate: null,
                    BitRateString: null, Channels: null, ChannelsString: null,
                    ChannelPositions: null, ChannelPositionsString2: null, ChannelLayout: null,
                    SamplesPerFrame: null, SamplingRate: null, SamplingRateString: null,
                    SamplingCount: null, FrameRate: null, FrameRateString: null,
                    FrameCount: null, CompressionMode: null, CompressionModeString: null,
                    Delay: null, DelayString: null, StreamSize: null, StreamSizeString: null,
                    StreamSizeProportion: null, Title: null, LanguageString: null,
                    LanguageString1: null, LanguageString2: null, LanguageString3: null,
                    LanguageString4: null, ServiceKind: null, ServiceKindString: null,
                    Default: null, DefaultString: null, Forced: null, ForcedString: null,
                    Extra: null));
            }
            
            // Add video tracks
            for (int i = 0; i < videoCount; i++)
            {
                tracks.Add(new MediaInfoResult.TrackInfo(
                    Type: TrackType.Video,
                    StreamKindID: i,
                    Language: null,
                    TypeOrder: null, Count: null, StreamCount: null, StreamKind: null,
                    StreamKindString: null, StreamKindPos: null, StreamOrder: null,
                    ID: null, IDString: null, UniqueID: null, Format: null,
                    FormatString: null, FormatInfo: null, FormatUrl: null,
                    FormatCommercial: null, FormatCommercialIfAny: null, FormatProfile: null,
                    FormatLevel: null, FormatTier: null, InternetMediaType: null,
                    CodecID: null, CodecIDInfo: null, CodecIDUrl: null, Duration: null,
                    DurationString: null, DurationString1: null, DurationString2: null,
                    DurationString3: null, DurationString4: null, DurationString5: null,
                    BitRateMode: null, BitRateModeString: null, BitRate: null,
                    BitRateString: null, Channels: null, ChannelsString: null,
                    ChannelPositions: null, ChannelPositionsString2: null, ChannelLayout: null,
                    SamplesPerFrame: null, SamplingRate: null, SamplingRateString: null,
                    SamplingCount: null, FrameRate: null, FrameRateString: null,
                    FrameCount: null, CompressionMode: null, CompressionModeString: null,
                    Delay: null, DelayString: null, StreamSize: null, StreamSizeString: null,
                    StreamSizeProportion: null, Title: null, LanguageString: null,
                    LanguageString1: null, LanguageString2: null, LanguageString3: null,
                    LanguageString4: null, ServiceKind: null, ServiceKindString: null,
                    Default: null, DefaultString: null, Forced: null, ForcedString: null,
                    Extra: null));
            }
            
            // Add subtitle tracks
            for (int i = 0; i < subtitleCount; i++)
            {
                tracks.Add(new MediaInfoResult.TrackInfo(
                    Type: TrackType.Text,
                    StreamKindID: i,
                    Language: null,
                    TypeOrder: null, Count: null, StreamCount: null, StreamKind: null,
                    StreamKindString: null, StreamKindPos: null, StreamOrder: null,
                    ID: null, IDString: null, UniqueID: null, Format: null,
                    FormatString: null, FormatInfo: null, FormatUrl: null,
                    FormatCommercial: null, FormatCommercialIfAny: null, FormatProfile: null,
                    FormatLevel: null, FormatTier: null, InternetMediaType: null,
                    CodecID: null, CodecIDInfo: null, CodecIDUrl: null, Duration: null,
                    DurationString: null, DurationString1: null, DurationString2: null,
                    DurationString3: null, DurationString4: null, DurationString5: null,
                    BitRateMode: null, BitRateModeString: null, BitRate: null,
                    BitRateString: null, Channels: null, ChannelsString: null,
                    ChannelPositions: null, ChannelPositionsString2: null, ChannelLayout: null,
                    SamplesPerFrame: null, SamplingRate: null, SamplingRateString: null,
                    SamplingCount: null, FrameRate: null, FrameRateString: null,
                    FrameCount: null, CompressionMode: null, CompressionModeString: null,
                    Delay: null, DelayString: null, StreamSize: null, StreamSizeString: null,
                    StreamSizeProportion: null, Title: null, LanguageString: null,
                    LanguageString1: null, LanguageString2: null, LanguageString3: null,
                    LanguageString4: null, ServiceKind: null, ServiceKindString: null,
                    Default: null, DefaultString: null, Forced: null, ForcedString: null,
                    Extra: null));
            }
            
            return new ScannedFileInfo
            {
                Path = "test_file.mkv",
                Result = new MediaInfoResult(
                    new MediaInfoResult.CreatingLibraryInfo("TestLib", "1.0", string.Empty),
                    new MediaInfoResult.MediaInfo("TestRef", tracks))
            };
        }
    }
}