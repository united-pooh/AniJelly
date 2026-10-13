using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Api.Controllers;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class DeprecatedRoutesTests
{
    private static readonly IReadOnlyList<ControllerActionDescriptor> _actions = GetActions();

    [Theory]
    [InlineData("GET", "Artists")]
    [InlineData("GET", "Artists/AlbumArtists")]
    [InlineData("GET", "Artists/{name}")]
    [InlineData("POST", "Users/{userId}/Images/{imageType}")]
    [InlineData("POST", "Users/{userId}/Images/{imageType}/{index}")]
    [InlineData("DELETE", "Users/{userId}/Images/{imageType}")]
    [InlineData("DELETE", "Users/{userId}/Images/{imageType}/{index}")]
    [InlineData("GET", "Users/{userId}/Images/{imageType}")]
    [InlineData("HEAD", "Users/{userId}/Images/{imageType}")]
    [InlineData("GET", "Users/{userId}/Images/{imageType}/{imageIndex}")]
    [InlineData("HEAD", "Users/{userId}/Images/{imageType}/{imageIndex}")]
    [InlineData("GET", "MusicGenres/{name}/InstantMix")]
    [InlineData("GET", "Artists/InstantMix")]
    [InlineData("GET", "MusicGenres/InstantMix")]
    [InlineData("GET", "Users/{userId}/Items")]
    [InlineData("GET", "Users/{userId}/Items/Resume")]
    [InlineData("GET", "Users/{userId}/Items/{itemId}/UserData")]
    [InlineData("POST", "Users/{userId}/Items/{itemId}/UserData")]
    [InlineData("GET", "LiveTv/Recordings/Series")]
    [InlineData("GET", "LiveTv/Recordings/Groups")]
    [InlineData("GET", "MusicGenres")]
    [InlineData("GET", "MusicGenres/{genreName}")]
    [InlineData("POST", "Users/{userId}/PlayedItems/{itemId}")]
    [InlineData("DELETE", "Users/{userId}/PlayedItems/{itemId}")]
    [InlineData("POST", "PlayingItems/{itemId}")]
    [InlineData("POST", "Users/{userId}/PlayingItems/{itemId}")]
    [InlineData("POST", "PlayingItems/{itemId}/Progress")]
    [InlineData("POST", "Users/{userId}/PlayingItems/{itemId}/Progress")]
    [InlineData("DELETE", "PlayingItems/{itemId}")]
    [InlineData("DELETE", "Users/{userId}/PlayingItems/{itemId}")]
    [InlineData("GET", "Startup/Configuration")]
    [InlineData("POST", "Startup/Configuration")]
    [InlineData("POST", "Startup/RemoteAccess")]
    [InlineData("GET", "Startup/User")]
    [InlineData("GET", "Startup/FirstUser")]
    [InlineData("GET", "Users/{userId}/Suggestions")]
    [InlineData("GET", "Trailers")]
    [InlineData("POST", "Users/{userId}/Authenticate")]
    [InlineData("POST", "Users/{userId}/Password")]
    [InlineData("POST", "Users/{userId}")]
    [InlineData("POST", "Users/{userId}/Configuration")]
    [InlineData("GET", "Users/{userId}/Items/{itemId}")]
    [InlineData("GET", "Users/{userId}/Items/Root")]
    [InlineData("GET", "Users/{userId}/Items/{itemId}/Intros")]
    [InlineData("POST", "Users/{userId}/FavoriteItems/{itemId}")]
    [InlineData("DELETE", "Users/{userId}/FavoriteItems/{itemId}")]
    [InlineData("DELETE", "Users/{userId}/Items/{itemId}/Rating")]
    [InlineData("POST", "Users/{userId}/Items/{itemId}/Rating")]
    [InlineData("GET", "Users/{userId}/Items/{itemId}/LocalTrailers")]
    [InlineData("GET", "Users/{userId}/Items/{itemId}/SpecialFeatures")]
    [InlineData("GET", "Users/{userId}/Items/Latest")]
    [InlineData("GET", "Users/{userId}/Views")]
    [InlineData("GET", "Users/{userId}/GroupingOptions")]
    public void RetiredRoute_IsNotRegistered(string method, string route)
    {
        Assert.DoesNotContain(_actions, action => Matches(action, method, route));
    }

    [Theory]
    [InlineData("POST", "LiveStreams/Open")]
    [InlineData("POST", "LiveStreams/Close")]
    [InlineData("GET", "Items/{itemId}/PlaybackInfo")]
    [InlineData("GET", "Items")]
    [InlineData("GET", "Items/{itemId}")]
    [InlineData("GET", "UserItems/Resume")]
    [InlineData("GET", "UserItems/{itemId}/UserData")]
    [InlineData("POST", "UserItems/{itemId}/UserData")]
    [InlineData("GET", "UserViews")]
    [InlineData("GET", "UserViews/GroupingOptions")]
    [InlineData("GET", "Items/Suggestions")]
    [InlineData("GET", "Genres")]
    [InlineData("GET", "Persons")]
    [InlineData("GET", "Items/{itemId}/InstantMix")]
    [InlineData("POST", "Users/AuthenticateByName")]
    [InlineData("POST", "Users/Password")]
    [InlineData("POST", "Users")]
    [InlineData("POST", "Users/Configuration")]
    [InlineData("POST", "Startup/User")]
    [InlineData("POST", "Startup/Complete")]
    [InlineData("POST", "UserPlayedItems/{itemId}")]
    [InlineData("DELETE", "UserPlayedItems/{itemId}")]
    [InlineData("POST", "Sessions/Playing")]
    [InlineData("POST", "Sessions/Playing/Progress")]
    [InlineData("POST", "Sessions/Playing/Stopped")]
    [InlineData("GET", "Videos/{itemId}/master.m3u8")]
    [InlineData("GET", "Videos/{itemId}/hls/{playlistId}/stream.m3u8")]
    public void SupportedRoute_RemainsRegistered(string method, string route)
    {
        Assert.Contains(_actions, action => Matches(action, method, route));
    }

    [Fact]
    public void LiveTvAndDvrRoutes_AreNotRegistered()
    {
        Assert.DoesNotContain(_actions, action =>
            action.AttributeRouteInfo?.Template?.StartsWith("LiveTv/", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void PluginChannelRoutes_AreNotRegistered()
    {
        Assert.DoesNotContain(_actions, action =>
            string.Equals(action.ControllerName, "Channels", StringComparison.OrdinalIgnoreCase));
    }

    private static bool Matches(ControllerActionDescriptor action, string method, string route)
    {
        return string.Equals(action.AttributeRouteInfo?.Template, route, StringComparison.OrdinalIgnoreCase)
            && action.ActionConstraints is not null
            && action.ActionConstraints.OfType<HttpMethodActionConstraint>()
                .Any(constraint => constraint.HttpMethods.Contains(method, StringComparer.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<ControllerActionDescriptor> GetActions()
    {
        using var services = new ServiceCollection()
            .AddLogging()
            .AddControllers()
            .AddApplicationPart(typeof(ItemsController).Assembly)
            .Services.BuildServiceProvider();

        return services.GetRequiredService<IActionDescriptorCollectionProvider>()
            .ActionDescriptors.Items.OfType<ControllerActionDescriptor>().ToArray();
    }
}
