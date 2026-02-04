using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PatchNotes.Data;

namespace PatchNotes.Tests;

public class WatchlistApiTests : IAsyncLifetime
{
    private PatchNotesApiFixture _fixture = null!;
    private HttpClient _client = null!;
    private HttpClient _authClient = null!;

    public async Task InitializeAsync()
    {
        _fixture = new PatchNotesApiFixture();
        await _fixture.InitializeAsync();
        _client = _fixture.CreateClient();
        _authClient = _fixture.CreateAuthenticatedClient();

        // Create test user and packages
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        db.Users.Add(new User
        {
            StytchUserId = PatchNotesApiFixture.TestUserId,
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        db.Packages.AddRange(
            new Package { Id = "pkg_react", Name = "react", Url = "https://github.com/facebook/react", NpmName = "react", GithubOwner = "facebook", GithubRepo = "react", CreatedAt = DateTime.UtcNow },
            new Package { Id = "pkg_vue", Name = "vue", Url = "https://github.com/vuejs/core", NpmName = "vue", GithubOwner = "vuejs", GithubRepo = "core", CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        _authClient.Dispose();
        await _fixture.DisposeAsync();
        _fixture.Dispose();
    }

    [Fact]
    public async Task GetWatchlist_RequiresAuthentication()
    {
        var response = await _client.GetAsync("/api/watchlist");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWatchlist_ReturnsEmptyArray_ForNewUser()
    {
        var response = await _authClient.GetAsync("/api/watchlist");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ids = await response.Content.ReadFromJsonAsync<string[]>();
        ids.Should().BeEmpty();
    }

    [Fact]
    public async Task PutWatchlist_SetsWatchlist()
    {
        var response = await _authClient.PutAsJsonAsync("/api/watchlist", new { packageIds = new[] { "pkg_react", "pkg_vue" } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ids = await response.Content.ReadFromJsonAsync<string[]>();
        ids.Should().BeEquivalentTo(["pkg_react", "pkg_vue"]);

        // Verify GET returns same
        var getResponse = await _authClient.GetAsync("/api/watchlist");
        var getIds = await getResponse.Content.ReadFromJsonAsync<string[]>();
        getIds.Should().BeEquivalentTo(["pkg_react", "pkg_vue"]);
    }

    [Fact]
    public async Task PutWatchlist_ReplacesExistingWatchlist()
    {
        // Set initial
        await _authClient.PutAsJsonAsync("/api/watchlist", new { packageIds = new[] { "pkg_react", "pkg_vue" } });

        // Replace
        var response = await _authClient.PutAsJsonAsync("/api/watchlist", new { packageIds = new[] { "pkg_vue" } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ids = await response.Content.ReadFromJsonAsync<string[]>();
        ids.Should().BeEquivalentTo(["pkg_vue"]);
    }

    [Fact]
    public async Task PostWatchlist_AddsSinglePackage()
    {
        var response = await _authClient.PostAsync("/api/watchlist/pkg_react", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var getResponse = await _authClient.GetAsync("/api/watchlist");
        var ids = await getResponse.Content.ReadFromJsonAsync<string[]>();
        ids.Should().Contain("pkg_react");
    }

    [Fact]
    public async Task PostWatchlist_Returns409_ForDuplicate()
    {
        await _authClient.PostAsync("/api/watchlist/pkg_react", null);

        var response = await _authClient.PostAsync("/api/watchlist/pkg_react", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        error.GetProperty("error").GetString().Should().Be("Already watching this package");
    }

    [Fact]
    public async Task PostWatchlist_Returns404_ForNonexistentPackage()
    {
        var response = await _authClient.PostAsync("/api/watchlist/pkg_nonexistent", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteWatchlist_RemovesPackage()
    {
        await _authClient.PostAsync("/api/watchlist/pkg_react", null);
        await _authClient.PostAsync("/api/watchlist/pkg_vue", null);

        var response = await _authClient.DeleteAsync("/api/watchlist/pkg_react");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _authClient.GetAsync("/api/watchlist");
        var ids = await getResponse.Content.ReadFromJsonAsync<string[]>();
        ids.Should().BeEquivalentTo(["pkg_vue"]);
    }

    [Fact]
    public async Task DeleteWatchlist_Returns204_EvenIfNotWatching()
    {
        var response = await _authClient.DeleteAsync("/api/watchlist/pkg_react");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
