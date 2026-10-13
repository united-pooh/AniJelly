using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public sealed class GenresControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;

    public GenresControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetGenres_NonexistentSearchTerm_ReturnsEmptyResult()
    {
        using var client = _factory.CreateClient();
        var accessToken = await AuthHelper.CompleteStartupAsync(client);
        client.DefaultRequestHeaders.AddAuthHeader(accessToken);

        using var response = await client.GetAsync($"Genres?searchTerm={Guid.NewGuid():N}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var genres = await response.Content.ReadFromJsonAsync<QueryResult<BaseItemDto>>(JsonDefaults.Options, TestContext.Current.CancellationToken);
        Assert.NotNull(genres);
        Assert.Empty(genres.Items);
        Assert.Equal(0, genres.TotalRecordCount);
    }
}
