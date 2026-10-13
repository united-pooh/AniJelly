using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    public class MediaInfoHelperTests
    {
        private static MediaInfoHelper CreateHelper(IMediaSourceManager? mediaSourceManager = null)
        {
            return new MediaInfoHelper(
                Mock.Of<IUserManager>(),
                Mock.Of<ILibraryManager>(),
                mediaSourceManager ?? Mock.Of<IMediaSourceManager>(),
                Mock.Of<IMediaEncoder>(),
                Mock.Of<IServerConfigurationManager>(),
                Mock.Of<ILogger<MediaInfoHelper>>(),
                Mock.Of<INetworkManager>(),
                Mock.Of<IDeviceManager>());
        }

        private static MediaSourceInfo CreateSource(Guid itemId, int bitrate, bool supportsDirectPlay = true)
        {
            return new MediaSourceInfo
            {
                Id = itemId.ToString("N", CultureInfo.InvariantCulture),
                Protocol = MediaProtocol.File,
                Bitrate = bitrate,
                SupportsDirectPlay = supportsDirectPlay,
                SupportsDirectStream = true,
                SupportsTranscoding = true
            };
        }

        [Fact]
        public void SortMediaSources_PreferredItemExceedsBitrate_StaysDefault()
        {
            // The version the user was watching (the queried item) must stay the default
            // even when a sibling version fits the bitrate limit better, since the resume
            // position belongs to that exact version.
            var preferredItemId = Guid.NewGuid();
            var preferredSource = CreateSource(preferredItemId, bitrate: 80_000_000, supportsDirectPlay: false);
            var siblingSource = CreateSource(Guid.NewGuid(), bitrate: 8_000_000);

            var result = new PlaybackInfoResponse
            {
                MediaSources = [siblingSource, preferredSource]
            };

            CreateHelper().SortMediaSources(result, maxBitrate: 20_000_000, preferredItemId);

            Assert.Equal(preferredSource.Id, result.MediaSources[0].Id);
        }

        [Fact]
        public void SortMediaSources_NoPreferredItem_OrdersByPlayability()
        {
            var directPlay = CreateSource(Guid.NewGuid(), bitrate: 8_000_000);
            var transcodeOnly = CreateSource(Guid.NewGuid(), bitrate: 8_000_000, supportsDirectPlay: false);
            transcodeOnly.SupportsDirectStream = false;

            var result = new PlaybackInfoResponse
            {
                MediaSources = [transcodeOnly, directPlay]
            };

            CreateHelper().SortMediaSources(result, maxBitrate: 20_000_000);

            Assert.Equal(directPlay.Id, result.MediaSources[0].Id);
        }

        [Fact]
        public void SortMediaSources_PreferredIdNotInSources_KeepsPlayabilityOrder()
        {
            var directPlay = CreateSource(Guid.NewGuid(), bitrate: 8_000_000);
            var transcodeOnly = CreateSource(Guid.NewGuid(), bitrate: 8_000_000, supportsDirectPlay: false);
            transcodeOnly.SupportsDirectStream = false;

            var result = new PlaybackInfoResponse
            {
                MediaSources = [transcodeOnly, directPlay]
            };

            CreateHelper().SortMediaSources(result, maxBitrate: 20_000_000, Guid.NewGuid());

            Assert.Equal(directPlay.Id, result.MediaSources[0].Id);
        }

        [Theory]
        [InlineData(MediaProtocol.Http, "https://media.example.com/video/stream.ts")]
        [InlineData(MediaProtocol.File, "/media/movie.mkv")]
        public async Task OpenMediaSource_PluginSource_PreservesPath(MediaProtocol protocol, string path)
        {
            var mediaSource = new MediaSourceInfo
            {
                Id = "source",
                Protocol = protocol,
                Path = path,
                LiveStreamId = "plugin-stream"
            };
            var request = new LiveStreamRequest { OpenToken = "plugin-token" };
            var mediaSourceManager = new Mock<IMediaSourceManager>();
            mediaSourceManager
                .Setup(x => x.OpenLiveStream(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LiveStreamResponse(mediaSource));

            var response = await CreateHelper(mediaSourceManager.Object)
                .OpenMediaSource(new DefaultHttpContext(), request).ConfigureAwait(true);

            Assert.Equal(path, response.MediaSource.Path);
            Assert.Equal("plugin-stream", response.MediaSource.LiveStreamId);
            mediaSourceManager.Verify(x => x.OpenLiveStream(request, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetPlaybackInfo_PluginSource_ReturnsIndependentCopy()
        {
            var source = new MediaSourceInfo
            {
                Id = "source",
                Protocol = MediaProtocol.Http,
                Path = "https://media.example.com/video/stream.ts",
                RequiresOpening = true,
                OpenToken = "plugin-token"
            };
            var item = new Movie();
            var mediaSourceManager = new Mock<IMediaSourceManager>();
            mediaSourceManager
                .Setup(x => x.GetPlaybackMediaSources(item, null, true, true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { source });

            var info = await CreateHelper(mediaSourceManager.Object).GetPlaybackInfo(item, null).ConfigureAwait(true);

            var returnedSource = Assert.Single(info.MediaSources);
            Assert.NotSame(source, returnedSource);
            Assert.Equal(source.Path, returnedSource.Path);
            Assert.True(returnedSource.RequiresOpening);
            Assert.Equal("plugin-token", returnedSource.OpenToken);
            returnedSource.Path = "changed";
            Assert.Equal("https://media.example.com/video/stream.ts", source.Path);
        }
    }
}
