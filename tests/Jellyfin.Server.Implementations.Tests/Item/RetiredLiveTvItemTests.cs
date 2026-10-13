using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class RetiredLiveTvItemTests : SqliteDbTestFixture
{
    private readonly BaseItemRepository _repository;
    private readonly Guid _movieId = Guid.NewGuid();
    private readonly Guid _channelMovieId = Guid.NewGuid();

    public RetiredLiveTvItemTests()
    {
        var lookup = new ItemTypeLookup();
        _repository = CreateBaseItemRepository(lookup);

        using var context = CreateDbContext();
        context.BaseItems.AddRange(
            new BaseItemEntity
            {
                Id = Guid.NewGuid(),
                Type = "MediaBrowser.Controller.LiveTv.LiveTvChannel",
                Name = "Retired channel",
                SortName = "a"
            },
            new BaseItemEntity
            {
                Id = Guid.NewGuid(),
                Type = "MediaBrowser.Controller.LiveTv.LiveTvProgram",
                Name = "Retired program",
                SortName = "b"
            },
            new BaseItemEntity
            {
                Id = Guid.NewGuid(),
                Type = "MediaBrowser.Controller.Channels.Channel",
                Name = "Retired plugin channel",
                SortName = "b1"
            },
            new BaseItemEntity
            {
                Id = _channelMovieId,
                Type = lookup.BaseItemKindNames[BaseItemKind.Movie],
                ChannelId = Guid.NewGuid(),
                Name = "Retired plugin movie",
                SortName = "b2"
            },
            new BaseItemEntity
            {
                Id = _movieId,
                Type = lookup.BaseItemKindNames[BaseItemKind.Movie],
                Name = "Kept movie",
                SortName = "c"
            });
        context.SaveChanges();
    }

    [Fact]
    public void GetItems_RetiredRows_DoNotConsumePageOrCount()
    {
        var result = _repository.GetItems(new InternalItemsQuery
        {
            Limit = 1,
            EnableTotalRecordCount = true,
            ExcludeItemIds = [BaseItemRepository.PlaceholderId],
            OrderBy = [(ItemSortBy.SortName, SortOrder.Ascending)],
            DtoOptions = DtoOptions.StoredColumnsOnly
        });

        Assert.Equal(1, result.TotalRecordCount);
        Assert.Equal(_movieId, Assert.Single(result.Items).Id);

        using var context = CreateDbContext();
        Assert.Equal(5, context.BaseItems.Count(e => !e.Id.Equals(BaseItemRepository.PlaceholderId)));
    }

    [Fact]
    public void RetrieveItem_RetiredPluginMovie_ReturnsNull()
    {
        Assert.Null(_repository.RetrieveItem(_channelMovieId));
    }

    [Fact]
    public void GetItems_EmptyChannelId_KeepsLibraryItem()
    {
        using (var context = CreateDbContext())
        {
            context.BaseItems.Single(e => e.Id.Equals(_movieId)).ChannelId = Guid.Empty;
            context.SaveChanges();
        }

        var result = _repository.GetItems(new InternalItemsQuery
        {
            ItemIds = [_movieId],
            EnableTotalRecordCount = true,
            DtoOptions = DtoOptions.StoredColumnsOnly
        });

        Assert.Equal(1, result.TotalRecordCount);
        Assert.Equal(_movieId, Assert.Single(result.Items).Id);
        Assert.Equal(_movieId, _repository.RetrieveItem(_movieId)?.Id);
    }

    [Fact]
    public void ApplyAccessFiltering_RetiredRows_ReturnsOnlyLibraryItem()
    {
        using var context = CreateDbContext();
        var ids = _repository.ApplyAccessFiltering(
                context,
                context.BaseItems.Where(e => !e.Id.Equals(BaseItemRepository.PlaceholderId)),
                new InternalItemsQuery())
            .Select(e => e.Id)
            .ToArray();

        Assert.Equal(_movieId, Assert.Single(ids));
    }

    [Fact]
    public void GetLatestItemList_RetiredPluginAlbum_DoesNotConsumeLimit()
    {
        var libraryId = Guid.NewGuid();
        var albumId = Guid.NewGuid();
        var lookup = new ItemTypeLookup();
        using (var context = CreateDbContext())
        {
            context.BaseItems.AddRange(
                new BaseItemEntity
                {
                    Id = Guid.NewGuid(),
                    Type = lookup.BaseItemKindNames[BaseItemKind.MusicAlbum],
                    ChannelId = Guid.NewGuid(),
                    TopParentId = libraryId,
                    Name = "Retired plugin album",
                    DateCreated = DateTime.UtcNow
                },
                new BaseItemEntity
                {
                    Id = albumId,
                    Type = lookup.BaseItemKindNames[BaseItemKind.MusicAlbum],
                    TopParentId = libraryId,
                    Name = "Kept album",
                    DateCreated = DateTime.UtcNow.AddDays(-1)
                });
            context.SaveChanges();
        }

        var items = _repository.GetLatestItemList(
            new InternalItemsQuery
            {
                TopParentIds = [libraryId],
                Limit = 1,
                DtoOptions = DtoOptions.StoredColumnsOnly
            },
            CollectionType.music);

        Assert.Equal(albumId, Assert.Single(items).Id);
    }

    [Fact]
    public void GetItemIdsList_RetiredRows_ReturnsOnlyLibraryItem()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery());

        Assert.Equal(_movieId, Assert.Single(ids));
    }

    [Theory]
    [InlineData(BaseItemKind.LiveTvChannel)]
    [InlineData(BaseItemKind.LiveTvProgram)]
    [InlineData(BaseItemKind.TvChannel)]
    [InlineData(BaseItemKind.TvProgram)]
    [InlineData(BaseItemKind.Channel)]
    public void GetItemIdsList_RetiredTypeQuery_ReturnsEmpty(BaseItemKind kind)
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery
        {
            IncludeItemTypes = [kind]
        });

        Assert.Empty(ids);
    }
}
