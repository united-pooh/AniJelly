using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.Migrations.Routines;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Jellyfin.Server.Integration.Tests;

public sealed class CleanupOrphanedExtrasTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;

    public CleanupOrphanedExtrasTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task PerformAsync_RetiredTypes_PreservesRowsAndDeletesOnlyKnownExtras(bool includeKnownExtra, bool emptyChannelId)
    {
        var dbFactory = _factory.Services.GetRequiredService<IDbContextFactory<JellyfinDbContext>>();
        var retiredItems = new[]
        {
            "MediaBrowser.Controller.LiveTv.LiveTvChannel",
            "MediaBrowser.Controller.LiveTv.LiveTvProgram",
            "MediaBrowser.Controller.Channels.Channel"
        }.Select(type => new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = type,
            OwnerId = BaseItemRepository.PlaceholderId
        }).ToArray();
        var ordinaryExtra = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = typeof(Movie).FullName!,
            ChannelId = emptyChannelId ? Guid.Empty : null,
            OwnerId = BaseItemRepository.PlaceholderId
        };
        var retiredPluginExtra = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = typeof(Movie).FullName!,
            ChannelId = Guid.NewGuid(),
            OwnerId = BaseItemRepository.PlaceholderId
        };

        await using (var context = await dbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            context.BaseItems.AddRange(retiredItems);
            context.BaseItems.Add(retiredPluginExtra);
            if (includeKnownExtra)
            {
                context.BaseItems.Add(ordinaryExtra);
            }

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var library = new Mock<ILibraryManager>(MockBehavior.Strict);
        library.Setup(manager => manager.DeleteItemsUnsafeFast(It.IsAny<IReadOnlyCollection<BaseItem>>(), false))
            .Callback<IReadOnlyCollection<BaseItem>, bool>((items, _) =>
            {
                Assert.All(items, item => Assert.NotNull(item));
                Assert.Equal(ordinaryExtra.Id, Assert.Single(items).Id);

                using var context = dbFactory.CreateDbContext();
                context.BaseItems.Remove(context.BaseItems.Single(item => item.Id.Equals(ordinaryExtra.Id)));
                context.SaveChanges();
            });

        var migration = new CleanupOrphanedExtras(
            Mock.Of<IStartupLogger<CleanupOrphanedExtras>>(),
            dbFactory,
            library.Object);
        await migration.PerformAsync(TestContext.Current.CancellationToken);

        library.Verify(manager => manager.DeleteItemsUnsafeFast(It.IsAny<IReadOnlyCollection<BaseItem>>(), false), includeKnownExtra ? Times.Once() : Times.Never());
        await using var verification = await dbFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.False(await verification.BaseItems.AnyAsync(item => item.Id.Equals(ordinaryExtra.Id), TestContext.Current.CancellationToken));
        var retiredIds = retiredItems.Select(item => item.Id).Append(retiredPluginExtra.Id).ToArray();
        Assert.Equal(retiredIds.Length, await verification.BaseItems.CountAsync(item => retiredIds.Contains(item.Id), TestContext.Current.CancellationToken));
    }
}
