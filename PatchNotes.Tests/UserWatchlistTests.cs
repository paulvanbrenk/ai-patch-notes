using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PatchNotes.Api.Routes;
using PatchNotes.Data;

namespace PatchNotes.Tests;

public class UserWatchlistTests : IAsyncLifetime
{
    private PatchNotesApiFixture _fixture = null!;
    private HttpClient _authClient = null!;

    // Packages that exist in the DB and are in the default watchlist
    private readonly List<Package> _defaultPackages = [];

    public async Task InitializeAsync()
    {
        _fixture = new PatchNotesApiFixture();

        // Configure 6 default watchlist packages (more than FreeWatchlistLimit=5)
        _fixture.ConfigureSettings(builder =>
        {
            builder.UseSetting("DefaultWatchlist:Packages:0", "dotnet/runtime");
            builder.UseSetting("DefaultWatchlist:Packages:1", "dotnet/aspnetcore");
            builder.UseSetting("DefaultWatchlist:Packages:2", "fastapi/fastapi");
            builder.UseSetting("DefaultWatchlist:Packages:3", "dotnet/efcore");
            builder.UseSetting("DefaultWatchlist:Packages:4", "steveyegge/beads");
            builder.UseSetting("DefaultWatchlist:Packages:5", "steveyegge/gastown");
        });

        await _fixture.InitializeAsync();
        _authClient = _fixture.CreateAuthenticatedClient();

        // Seed all 6 default packages into the DB
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();

        var packageData = new[]
        {
            ("dotnet-runtime", "dotnet", "runtime"),
            ("dotnet-aspnetcore", "dotnet", "aspnetcore"),
            ("fastapi", "fastapi", "fastapi"),
            ("dotnet-efcore", "dotnet", "efcore"),
            ("beads", "steveyegge", "beads"),
            ("gastown", "steveyegge", "gastown"),
        };

        foreach (var (name, owner, repo) in packageData)
        {
            var pkg = new Package
            {
                Name = name,
                Url = $"https://github.com/{owner}/{repo}",
                GithubOwner = owner,
                GithubRepo = repo,

            };
            db.Packages.Add(pkg);
            _defaultPackages.Add(pkg);
        }

        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _authClient.Dispose();
        await _fixture.DisposeAsync();
        _fixture.Dispose();
    }

    [Fact]
    public async Task HandleLogin_GivenNewUser_PopulatesWatchlistWithDefaults()
    {
        // Login as a new user (no user record exists yet)
        var loginResponse = await _authClient.PostAsync("/api/users/login", null);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check watchlist was auto-populated (capped at 5 for free user)
        var watchlistResponse = await _authClient.GetAsync("/api/watchlist");
        watchlistResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var watchlistPackages = await watchlistResponse.Content.ReadFromJsonAsync<WatchlistPackageDto[]>();

        watchlistPackages.Should().NotBeNull();
        watchlistPackages!.Length.Should().BeGreaterThan(0, "new user should have default packages");

        // All returned IDs should be from the default packages
        var defaultPackageIds = _defaultPackages.Select(p => p.Id).ToHashSet();
        watchlistPackages.Select(p => p.Id).Should().OnlyContain(id => defaultPackageIds.Contains(id));
    }

    [Fact]
    public async Task HandleLogin_GivenReturningUser_DoesNotModifyExistingWatchlist()
    {
        // Create user by logging in
        await _authClient.PostAsync("/api/users/login", null);

        // Replace watchlist with just one package
        var singlePackageId = _defaultPackages[2].Id; // fastapi
        await _authClient.PutAsJsonAsync("/api/watchlist", new { packageIds = new[] { singlePackageId } });

        // Login again (returning user)
        await _authClient.PostAsync("/api/users/login", null);

        // Watchlist should still be the single package, NOT re-populated
        var watchlistResponse = await _authClient.GetAsync("/api/watchlist");
        var watchlistPackages = await watchlistResponse.Content.ReadFromJsonAsync<WatchlistPackageDto[]>();

        watchlistPackages.Should().NotBeNull();
        watchlistPackages!.Select(p => p.Id).Should().BeEquivalentTo([singlePackageId]);
    }

    [Fact]
    public async Task HandleLogin_GivenNewFreeUser_CapsWatchlistAtFreeLimit()
    {
        // Use a non-admin client so the free tier limit applies
        // (admin users are treated as Pro and bypass the limit)
        using var freeClient = _fixture.CreateNonAdminClient();

        // We configured 6 default packages but free limit is 5
        var loginResponse = await freeClient.PostAsync("/api/users/login", null);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var watchlistResponse = await freeClient.GetAsync("/api/watchlist");
        var watchlistPackages = await watchlistResponse.Content.ReadFromJsonAsync<WatchlistPackageDto[]>();

        watchlistPackages.Should().NotBeNull();
        watchlistPackages!.Length.Should().Be(5, "free user should be capped at FreeWatchlistLimit");
    }

}

/// <summary>
/// Tests that missing default packages are skipped during auto-population.
/// Uses a separate fixture that only seeds 3 of the 6 configured defaults.
/// </summary>
public class UserWatchlistMissingPackagesTests : IAsyncLifetime
{
    private PatchNotesApiFixture _fixture = null!;
    private HttpClient _authClient = null!;
    private readonly List<Package> _seededPackages = [];

    public async Task InitializeAsync()
    {
        _fixture = new PatchNotesApiFixture();

        // Configure 6 default packages, but only seed 3 in the DB
        _fixture.ConfigureSettings(builder =>
        {
            builder.UseSetting("DefaultWatchlist:Packages:0", "dotnet/runtime");
            builder.UseSetting("DefaultWatchlist:Packages:1", "dotnet/aspnetcore");
            builder.UseSetting("DefaultWatchlist:Packages:2", "fastapi/fastapi");
            builder.UseSetting("DefaultWatchlist:Packages:3", "dotnet/efcore");
            builder.UseSetting("DefaultWatchlist:Packages:4", "steveyegge/beads");
            builder.UseSetting("DefaultWatchlist:Packages:5", "steveyegge/gastown");
        });

        await _fixture.InitializeAsync();
        _authClient = _fixture.CreateAuthenticatedClient();

        // Only seed 3 of the 6 configured packages
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();

        var partialData = new[]
        {
            ("dotnet-runtime", "dotnet", "runtime"),
            ("fastapi", "fastapi", "fastapi"),
            ("beads", "steveyegge", "beads"),
        };

        foreach (var (name, owner, repo) in partialData)
        {
            var pkg = new Package
            {
                Name = name,
                Url = $"https://github.com/{owner}/{repo}",
                GithubOwner = owner,
                GithubRepo = repo,

            };
            db.Packages.Add(pkg);
            _seededPackages.Add(pkg);
        }

        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _authClient.Dispose();
        await _fixture.DisposeAsync();
        _fixture.Dispose();
    }

    [Fact]
    public async Task HandleLogin_GivenMissingDefaultPackages_SkipsThem()
    {
        var loginResponse = await _authClient.PostAsync("/api/users/login", null);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var watchlistResponse = await _authClient.GetAsync("/api/watchlist");
        var watchlistPackages = await watchlistResponse.Content.ReadFromJsonAsync<WatchlistPackageDto[]>();

        watchlistPackages.Should().NotBeNull();
        watchlistPackages!.Length.Should().Be(3, "only the 3 packages that exist in DB should be added");

        var expectedIds = _seededPackages.Select(p => p.Id).ToHashSet();
        watchlistPackages.Select(p => p.Id).Should().OnlyContain(id => expectedIds.Contains(id));
    }
}
