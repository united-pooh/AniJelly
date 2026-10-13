using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Plugins.Omdb;
using MediaBrowser.Providers.Plugins.Tmdb.TV;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Jellyfin.Server.Integration.Tests;

public sealed class AnimeLibraryResolutionTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;

    public AnimeLibraryResolutionTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("第一季", 1)]
    [InlineData("第二季", 2)]
    [InlineData("第2期", 2)]
    [InlineData("SPs", 0)]
    [InlineData("特典", 0)]
    [InlineData("特别篇", 0)]
    [InlineData("续篇", null)]
    public void ResolveAnimeDirectory_SeparatesEpisodesClipsAndDiscs(string folder, int? expectedSeason)
    {
        var library = _factory.Services.GetRequiredService<ILibraryManager>();
        var fileSystem = _factory.Services.GetRequiredService<IFileSystem>();
        var root = Directory.CreateTempSubdirectory("anijelly-anime-");
        try
        {
            var seasonPath = Path.Combine(root.FullName, folder);
            Directory.CreateDirectory(Path.Combine(seasonPath, "NCED"));
            Directory.CreateDirectory(Path.Combine(seasonPath, "CDs"));
            foreach (var name in new[] { "Show - 01.mkv", "Show NCOP01.mkv", "NCED/01.mkv", "CDs/01.flac", "CDs/02.mkv" })
            {
                File.WriteAllBytes(Path.Combine(seasonPath, name), []);
            }

            var series = CreateSeries(library, root.FullName);
            var options = new LibraryOptions();
            var directoryService = new DirectoryService(fileSystem);
            var season = Assert.IsType<Season>(Assert.Single(library.ResolvePaths(
                fileSystem.GetFileSystemEntries(root.FullName), directoryService, series, options, CollectionType.tvshows)));
            Assert.Equal(expectedSeason, season.IndexNumber);
            library.RegisterItem(season);

            var files = fileSystem.GetFileSystemEntries(seasonPath).ToArray();
            var children = library.ResolvePaths(files, directoryService, season, options, CollectionType.tvshows).ToArray();
            var episode = Assert.Single(children.OfType<Episode>());
            library.FillMissingEpisodeNumbersFromPath(episode, false);
            Assert.Equal("Show - 01.mkv", Path.GetFileName(episode.Path));
            Assert.Equal(1, episode.IndexNumber);
            Assert.Equal(expectedSeason, episode.ParentIndexNumber);
            Assert.DoesNotContain(children, item => Path.GetFileName(item.Path) is "CDs" or "NCED");

            var clips = library.FindExtras(season, files, directoryService).ToArray();
            Assert.Equal(2, clips.Length);
            Assert.All(clips, clip =>
            {
                Assert.IsType<Video>(clip);
                Assert.Equal(ExtraType.Clip, clip.ExtraType);
            });

            library.FillMissingEpisodeNumbersFromPath(episode, false);
            Assert.Equal(expectedSeason, episode.ParentIndexNumber);

            episode.ParentIndexNumber = 7;
            library.FillMissingEpisodeNumbersFromPath(episode, false);
            Assert.Equal(7, episode.ParentIndexNumber);
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    public async Task TmdbMetadata_UnknownSeason_DoesNotUseClient(int? seasonNumber)
    {
        _ = _factory.Services.GetRequiredService<ILibraryManager>();
        var provider = new TmdbEpisodeProvider(Mock.Of<IHttpClientFactory>(), null!);
        var info = new EpisodeInfo
        {
            IndexNumber = 1,
            ParentIndexNumber = seasonNumber,
            SeriesProviderIds = new Dictionary<string, string> { ["Tmdb"] = "123" }
        };

        var result = await provider.GetMetadata(info, TestContext.Current.CancellationToken);

        Assert.False(result.HasMetadata);
        Assert.Null(result.Item);
        Assert.Empty(await provider.GetSearchResults(info, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    public async Task TmdbImages_UnknownSeason_DoesNotUseClient(int? seasonNumber)
    {
        var library = _factory.Services.GetRequiredService<ILibraryManager>();
        var series = CreateSeries(library, "/test/Show");
        var episode = new Episode { SeriesId = series.Id, ParentIndexNumber = seasonNumber, IndexNumber = 1 };
        var provider = new TmdbEpisodeImageProvider(Mock.Of<IHttpClientFactory>(), null!);

        Assert.Empty(await provider.GetImages(episode, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    public async Task OmdbMetadata_UnknownSeason_DoesNotReadCacheOrRequestMetadata(int? seasonNumber)
    {
        var http = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        var configuration = new Mock<IServerConfigurationManager>(MockBehavior.Strict);
        var provider = new OmdbEpisodeProvider(http.Object, Mock.Of<ILibraryManager>(), fileSystem.Object, configuration.Object);
        var info = new EpisodeInfo
        {
            IndexNumber = 1,
            ParentIndexNumber = seasonNumber,
            SeriesProviderIds = new Dictionary<string, string> { ["Imdb"] = "tt1234567" }
        };

        var result = await provider.GetMetadata(info, TestContext.Current.CancellationToken);

        Assert.False(result.HasMetadata);
        Assert.Null(result.Item.ParentIndexNumber);
        http.VerifyNoOtherCalls();
        fileSystem.VerifyNoOtherCalls();
        configuration.VerifyNoOtherCalls();
    }

    private static Series CreateSeries(ILibraryManager library, string path)
    {
        var series = new Series { Id = Guid.NewGuid(), Name = "Show", Path = path };
        series.SetProviderId(MetadataProvider.Tmdb, "123");
        library.RegisterItem(series);
        return series;
    }
}
