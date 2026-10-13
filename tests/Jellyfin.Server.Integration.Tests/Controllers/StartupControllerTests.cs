using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Models.StartupDtos;
using Jellyfin.Api.Models.UserDtos;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Model.Configuration;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers
{
    public sealed class StartupControllerTests : IClassFixture<JellyfinApplicationFactory>
    {
        private readonly JellyfinApplicationFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;

        public StartupControllerTests(JellyfinApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Startup_FromEmptyDatabase_ConfiguresAdministratorAndCompletesWizard()
        {
            using var client = _factory.CreateClient();
            var user = new StartupUserDto
            {
                Name = "NewName",
                Password = "NewPassword"
            };

            using var userResponse = await client.PostAsJsonAsync("/Startup/User", user, _jsonOptions, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NoContent, userResponse.StatusCode);

            using var request = new HttpRequestMessage(HttpMethod.Post, "/Users/AuthenticateByName");
            request.Headers.TryAddWithoutValidation(AuthHelper.AuthHeaderName, AuthHelper.DummyAuthHeader);
            request.Content = JsonContent.Create(new AuthenticateUserByName { Username = user.Name, Pw = user.Password }, options: _jsonOptions);
            using var authResponse = await client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, authResponse.StatusCode);
            Assert.Equal(MediaTypeNames.Application.Json, authResponse.Content.Headers.ContentType?.MediaType);

            var result = await authResponse.Content.ReadFromJsonAsync<AuthenticationResult>(_jsonOptions, TestContext.Current.CancellationToken);
            Assert.NotNull(result);
            Assert.Equal(user.Name, result.User.Name);
            Assert.True(result.User.Policy.IsAdministrator);
            Assert.NotEmpty(result.AccessToken);
            using var adminClient = _factory.CreateClient();
            adminClient.DefaultRequestHeaders.AddAuthHeader(result.AccessToken);

            using var initialResponse = await adminClient.GetAsync("/System/Configuration", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, initialResponse.StatusCode);
            var config = await initialResponse.Content.ReadFromJsonAsync<ServerConfiguration>(_jsonOptions, TestContext.Current.CancellationToken);
            Assert.NotNull(config);
            config.ServerName = "NewServer";
            config.UICulture = "nl-NL";
            config.MetadataCountryCode = "be";
            config.PreferredMetadataLanguage = "nl";

            using var postResponse = await adminClient.PostAsJsonAsync("/System/Configuration", config, _jsonOptions, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            using var getResponse = await adminClient.GetAsync("/System/Configuration", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            Assert.Equal(MediaTypeNames.Application.Json, getResponse.Content.Headers.ContentType?.MediaType);

            var newConfig = await getResponse.Content.ReadFromJsonAsync<ServerConfiguration>(_jsonOptions, TestContext.Current.CancellationToken);
            Assert.NotNull(newConfig);
            Assert.Equal(config.ServerName, newConfig.ServerName);
            Assert.Equal(config.UICulture, newConfig.UICulture);
            Assert.Equal(config.MetadataCountryCode, newConfig.MetadataCountryCode);
            Assert.Equal(config.PreferredMetadataLanguage, newConfig.PreferredMetadataLanguage);

            var replacementUser = new StartupUserDto { Name = "OtherName", Password = "OtherPassword" };
            using var overwriteResponse = await client.PostAsJsonAsync("/Startup/User", replacementUser, _jsonOptions, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Forbidden, overwriteResponse.StatusCode);

            using var completeResponse = await client.PostAsync("/Startup/Complete", new ByteArrayContent(Array.Empty<byte>()), TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NoContent, completeResponse.StatusCode);

            using var unauthorizedResponse = await client.PostAsJsonAsync("/Startup/User", replacementUser, _jsonOptions, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);
        }
    }
}
