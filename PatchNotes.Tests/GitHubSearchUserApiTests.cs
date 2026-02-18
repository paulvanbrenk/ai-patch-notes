using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using PatchNotes.Sync.Core.GitHub;
using PatchNotes.Sync.Core.GitHub.Models;

namespace PatchNotes.Tests;

public class GitHubSearchUserApiTests : IAsyncLifetime
{
    private PatchNotesApiFixture _fixture = null!;
    private HttpClient _authClient = null!;
    private HttpClient _unauthClient = null!;
    private HttpClient _nonAdminClient = null!;
    private Mock<IGitHubClient> _mockGitHubClient = null!;

    public async Task InitializeAsync()
    {
        _mockGitHubClient = new Mock<IGitHubClient>();
        _fixture = new PatchNotesApiFixture();
        _fixture.ConfigureServices(services =>
        {
            services.RemoveAll<IGitHubClient>();
            services.AddSingleton(_mockGitHubClient.Object);
        });
        await _fixture.InitializeAsync();
        _authClient = _fixture.CreateAuthenticatedClient();
        _unauthClient = _fixture.CreateClient();
        _nonAdminClient = _fixture.CreateNonAdminClient();
    }

    public async Task DisposeAsync()
    {
        _authClient.Dispose();
        _unauthClient.Dispose();
        _nonAdminClient.Dispose();
        await _fixture.DisposeAsync();
        _fixture.Dispose();
    }

    [Fact]
    public async Task SearchGitHubUser_ReturnsResults_ForAuthenticatedUser()
    {
        _mockGitHubClient
            .Setup(c => c.SearchRepositoriesAsync("react", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GitHubSearchResult>
            {
                new()
                {
                    FullName = "facebook/react",
                    Owner = new GitHubSearchOwner { Login = "facebook" },
                    Name = "react",
                    Description = "A JavaScript library",
                    StargazersCount = 200000
                }
            });

        var response = await _authClient.GetAsync("/api/github/search?q=react");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<JsonElement>();
        results.GetArrayLength().Should().Be(1);
        results[0].GetProperty("owner").GetString().Should().Be("facebook");
        results[0].GetProperty("repo").GetString().Should().Be("react");
        results[0].GetProperty("description").GetString().Should().Be("A JavaScript library");
        results[0].GetProperty("starCount").GetInt32().Should().Be(200000);
    }

    [Fact]
    public async Task SearchGitHubUser_ReturnsResults_ForNonAdminUser()
    {
        _mockGitHubClient
            .Setup(c => c.SearchRepositoriesAsync("vue", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GitHubSearchResult>
            {
                new()
                {
                    FullName = "vuejs/core",
                    Owner = new GitHubSearchOwner { Login = "vuejs" },
                    Name = "core",
                    Description = "Vue.js core",
                    StargazersCount = 50000
                }
            });

        var response = await _nonAdminClient.GetAsync("/api/github/search?q=vue");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<JsonElement>();
        results.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task SearchGitHubUser_Returns400_WhenQueryMissing()
    {
        var response = await _authClient.GetAsync("/api/github/search");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchGitHubUser_Returns400_WhenQueryTooShort()
    {
        var response = await _authClient.GetAsync("/api/github/search?q=a");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchGitHubUser_Returns401_WhenUnauthenticated()
    {
        var response = await _unauthClient.GetAsync("/api/github/search?q=react");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
