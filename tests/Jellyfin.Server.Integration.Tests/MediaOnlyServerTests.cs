using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Jellyfin.Server.Integration.Tests;

public sealed class MediaOnlyServerTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;

    public MediaOnlyServerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MediaServer_StartsWithoutTvAndChannelServices()
    {
        using var client = _factory.CreateClient();
        var token = await AuthHelper.CompleteStartupAsync(client);
        client.DefaultRequestHeaders.AddAuthHeader(token);

        Assert.DoesNotContain(
            _factory.Services.GetRequiredService<IApplicationHost>().GetExportTypes<IMediaSourceProvider>(),
            type => type.Name is "ChannelDynamicMediaSourceProvider" or "LiveTvMediaSourceProvider");
        var tasks = _factory.Services.GetRequiredService<ITaskManager>().ScheduledTasks;
        Assert.DoesNotContain(tasks, task => task.ScheduledTask.GetType().Name is "RefreshGuideScheduledTask" or "RefreshChannelsScheduledTask");
        Assert.DoesNotContain(_factory.Services.GetServices<IHostedService>(), service =>
            service.GetType().Name is "RecordingsHost" or "RecordingNotifier");

        foreach (var route in new[] { "UserViews", "Items" })
        {
            using var response = await client.GetAsync(route, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        using var specResponse = await client.GetAsync("api-docs/openapi.json", TestContext.Current.CancellationToken);
        specResponse.EnsureSuccessStatusCode();
        using var spec = JsonDocument.Parse(await specResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.DoesNotContain(spec.RootElement.GetProperty("paths").EnumerateObject(), path =>
            path.Name.StartsWith("/LiveTv", StringComparison.OrdinalIgnoreCase)
            || path.Name.StartsWith("/Channels", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(spec.RootElement.GetProperty("paths").EnumerateObject(), path => path.Name == "/LiveStreams/Open");

        foreach (var route in new[] { "LiveTv/Info", "Channels" })
        {
            using var retiredResponse = await client.GetAsync(route, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NotFound, retiredResponse.StatusCode);
        }
    }
}
