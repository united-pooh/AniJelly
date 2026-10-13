using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

public class ItemTypeLookupTests
{
    public static TheoryData<BaseItemKind> MappedKinds()
        => new(new ItemTypeLookup().BaseItemKindNames.Keys.Where(kind => kind is not (
            BaseItemKind.Channel or BaseItemKind.LiveTvChannel or BaseItemKind.LiveTvProgram or BaseItemKind.TvChannel or BaseItemKind.TvProgram)));

    [Theory]
    [MemberData(nameof(MappedKinds))]
    public void BaseItemKindNames_Kind_NamesAnItemType(BaseItemKind kind)
    {
        var name = new ItemTypeLookup().BaseItemKindNames[kind];

        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).FirstOrDefault(t => t is not null);

        Assert.NotNull(type);
        Assert.True(typeof(BaseItem).IsAssignableFrom(type), $"{name} is not an item type.");
    }

    [Theory]
    [InlineData(BaseItemKind.Channel, 6, "MediaBrowser.Controller.Channels.Channel")]
    [InlineData(BaseItemKind.LiveTvChannel, 14, "MediaBrowser.Controller.LiveTv.LiveTvChannel")]
    [InlineData(BaseItemKind.LiveTvProgram, 15, "MediaBrowser.Controller.LiveTv.LiveTvProgram")]
    [InlineData(BaseItemKind.TvChannel, 31, "MediaBrowser.Controller.LiveTv.LiveTvChannel")]
    [InlineData(BaseItemKind.TvProgram, 32, "MediaBrowser.Controller.LiveTv.LiveTvProgram")]
    public void BaseItemKindNames_RetiredKind_PreservesAliasWithoutActiveType(BaseItemKind kind, int historicalValue, string name)
    {
        Assert.Equal(historicalValue, (int)kind);
        Assert.Equal(name, new ItemTypeLookup().BaseItemKindNames[kind]);
        Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly => assembly.GetType(name) is not null);
    }
}
