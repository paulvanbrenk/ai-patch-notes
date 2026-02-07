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
    private int _reactPackageId;
    private int _vuePackageId;

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
        var react = new Package { Name = "react", Url = "https://github.com/facebook/react", NpmName = "react", GithubOwner = "facebook", GithubRepo = "react", CreatedAt = DateTime.UtcNow };
        var vue = new Package { Name = "vue", Url = "https://github.com/vuejs/core", NpmName = "vue", GithubOwner = "vuejs", GithubRepo = "core", CreatedAt = DateTime.UtcNow };
        db.Packages.AddRange(react, vue);
        await db.SaveChangesAsync();
        _reactPackageId = react.Id;
        _vuePackageId = vue.Id;
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
        var ids = await response.Content.ReadFromJsonAsync<int[]>();
        ids.Should().BeEmpty();
    }

    [Fact]
    public async Task PutWatchlist_SetsWatchlist()
    {
        var response = await _authClient.PutAsJsonAsync("/api/watchlist", new { packageIds = new[] { _reactPackageId, _vuePackageId } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ids = await response.Content.ReadFromJsonAsync<int[]>();
        ids.Should().BeEquivalentTo([_reactPackageId, _vuePackageId]);

        // Verify GET returns same
        var getResponse = await _authClient.GetAsync("/api/watchlist");
        var getIds = await getResponse.Content.ReadFromJsonAsync<int[]>();
        getIds.Should().BeEquivalentTo([_reactPackageId, _vuePackageId]);
    }

    [Fact]
    public async Task PutWatchlist_ReplacesExistingWatchlist()
    {
        // Set initial
        await _authClient.PutAsJsonAsync("/api/watchlist", new { packageIds = new[] { _reactPackageId, _vuePackageId } });

        // Replace
        var response = await _authClient.PutAsJsonAsync("/api/watchlist", new { packageIds = new[] { _vuePackageId } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ids = await response.Content.ReadFromJsonAsync<int[]>();
        ids.Should().BeEquivalentTo([_vuePackageId]);
    }

    [Fact]
    public async Task PostWatchlist_AddsSinglePackage()
    {
        var response = await _authClient.PostAsync($"/api/watchlist/{_reactPackageId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var getResponse = await _authClient.GetAsync("/api/watchlist");
        var ids = await getResponse.Content.ReadFromJsonAsync<int[]>();
        ids.Should().Contain(_reactPackageId);
    }

    [Fact]
    public async Task PostWatchlist_Returns409_ForDuplicate()
    {
        await _authClient.PostAsync($"/api/watchlist/{_reactPackageId}", null);

        var response = await _authClient.PostAsync($"/api/watchlist/{_reactPackageId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        error.GetProperty("error").GetString().Should().Be("Already watching this package");
    }

    [Fact]
    public async Task PostWatchlist_Returns404_ForNonexistentPackage()
    {
        var response = await _authClient.PostAsync("/api/watchlist/99999", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteWatchlist_RemovesPackage()
    {
        await _authClient.PostAsync($"/api/watchlist/{_reactPackageId}", null);
        await _authClient.PostAsync($"/api/watchlist/{_vuePackageId}", null);

        var response = await _authClient.DeleteAsync($"/api/watchlist/{_reactPackageId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _authClient.GetAsync("/api/watchlist");
        var ids = await getResponse.Content.ReadFromJsonAsync<int[]>();
        ids.Should().BeEquivalentTo([_vuePackageId]);
    }

    [Fact]
    public async Task DeleteWatchlist_Returns204_EvenIfNotWatching()
    {
        var response = await _authClient.DeleteAsync($"/api/watchlist/{_reactPackageId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
