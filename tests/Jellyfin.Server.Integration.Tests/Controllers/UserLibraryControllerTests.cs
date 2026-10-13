using System;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public sealed class UserLibraryControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
    private static string? _accessToken;

    public UserLibraryControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRootFolder_NonexistentUserId_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var response = await client.GetAsync($"Items/Root?userId={Guid.NewGuid()}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRootFolder_UserId_Valid()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        _ = await AuthHelper.GetRootFolderDtoAsync(client);
    }

    [Theory]
    [InlineData("Items/{1}?userId={0}")]
    [InlineData("Items/{1}/Intros?userId={0}")]
    [InlineData("Items/{1}/LocalTrailers?userId={0}")]
    [InlineData("Items/{1}/SpecialFeatures?userId={0}")]
    public async Task GetItem_NonexistentUserId_NotFound(string format)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var rootFolderDto = await AuthHelper.GetRootFolderDtoAsync(client);

        var response = await client.GetAsync(string.Format(CultureInfo.InvariantCulture, format, Guid.NewGuid(), rootFolderDto.Id), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("Items/{1}?userId={0}")]
    [InlineData("Items/{1}/Intros?userId={0}")]
    [InlineData("Items/{1}/LocalTrailers?userId={0}")]
    [InlineData("Items/{1}/SpecialFeatures?userId={0}")]
    public async Task GetItem_NonexistentItemId_NotFound(string format)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var userDto = await AuthHelper.GetUserDtoAsync(client);

        var response = await client.GetAsync(string.Format(CultureInfo.InvariantCulture, format, userDto.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(Skip = "Disabled for flaky execution after refactor.")]
    public async Task GetItem_UserIdAndItemId_Valid()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var userDto = await AuthHelper.GetUserDtoAsync(client);
        var rootFolderDto = await AuthHelper.GetRootFolderDtoAsync(client, userDto.Id);

        var response = await client.GetAsync($"Items/{rootFolderDto.Id}?userId={userDto.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rootDto = await response.Content.ReadFromJsonAsync<BaseItemDto>(_jsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(rootDto);
    }

    [Fact(Skip = "Disabled for flaky execution after refactor.")]
    public async Task GetIntros_UserIdAndItemId_Valid()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var userDto = await AuthHelper.GetUserDtoAsync(client);
        var rootFolderDto = await AuthHelper.GetRootFolderDtoAsync(client, userDto.Id);

        var response = await client.GetAsync($"Items/{rootFolderDto.Id}/Intros?userId={userDto.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rootDto = await response.Content.ReadFromJsonAsync<QueryResult<BaseItemDto>>(_jsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(rootDto);
    }

    [Theory(Skip = "Disabled for flaky execution after refactor.")]
    [InlineData("Items/{1}/LocalTrailers?userId={0}")]
    [InlineData("Items/{1}/SpecialFeatures?userId={0}")]
    public async Task LocalTrailersAndSpecialFeatures_UserIdAndItemId_Valid(string format)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var userDto = await AuthHelper.GetUserDtoAsync(client);
        var rootFolderDto = await AuthHelper.GetRootFolderDtoAsync(client, userDto.Id);

        var response = await client.GetAsync(string.Format(CultureInfo.InvariantCulture, format, userDto.Id, rootFolderDto.Id), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rootDto = await response.Content.ReadFromJsonAsync<BaseItemDto[]>(_jsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(rootDto);
    }
}
