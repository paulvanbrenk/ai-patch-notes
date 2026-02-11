using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PatchNotes.Data;

namespace PatchNotes.Tests;

public class ReleasesApiTests : IAsyncLifetime
{
    private PatchNotesApiFixture _fixture = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _fixture = new PatchNotesApiFixture();
        await _fixture.InitializeAsync();
        _client = _fixture.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _fixture.DisposeAsync();
        _fixture.Dispose();
    }

    [Fact]
    public async Task GetReleases_ReturnsEmptyList_WhenNoReleases()
    {
        var response = await _client.GetAsync("/api/releases");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var releases = await response.Content.ReadFromJsonAsync<JsonElement>();
        releases.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetReleases_ReturnsReleasesWithinDefaultDays()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var package = new Package { Name = "test-pkg", Url = "https://github.com/owner/repo", NpmName = "test-pkg", GithubOwner = "owner", GithubRepo = "repo", CreatedAt = DateTime.UtcNow };
        db.Packages.Add(package);
        await db.SaveChangesAsync();

        // Add release within 7 days (default)
        db.Releases.Add(new Release
        {
            PackageId = package.Id,
            Tag = "v1.0.0",
            Title = "Release 1",
            PublishedAt = DateTime.UtcNow.AddDays(-3),
            FetchedAt = DateTime.UtcNow
        });
        // Add release outside 7 days
        db.Releases.Add(new Release
        {
            PackageId = package.Id,
            Tag = "v0.9.0",
            Title = "Old Release",
            PublishedAt = DateTime.UtcNow.AddDays(-10),
            FetchedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/releases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var releases = await response.Content.ReadFromJsonAsync<JsonElement>();
        releases.GetArrayLength().Should().Be(1);
        releases[0].GetProperty("tag").GetString().Should().Be("v1.0.0");
    }

    [Fact]
    public async Task GetReleases_FiltersByDays()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var package = new Package { Name = "test-pkg", Url = "https://github.com/owner/repo", NpmName = "test-pkg", GithubOwner = "owner", GithubRepo = "repo", CreatedAt = DateTime.UtcNow };
        db.Packages.Add(package);
        await db.SaveChangesAsync();

        db.Releases.AddRange(
            new Release { PackageId = package.Id, Tag = "v1.0.0", PublishedAt = DateTime.UtcNow.AddDays(-3), FetchedAt = DateTime.UtcNow },
            new Release { PackageId = package.Id, Tag = "v0.9.0", PublishedAt = DateTime.UtcNow.AddDays(-10), FetchedAt = DateTime.UtcNow },
            new Release { PackageId = package.Id, Tag = "v0.8.0", PublishedAt = DateTime.UtcNow.AddDays(-25), FetchedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/releases?days=30");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var releases = await response.Content.ReadFromJsonAsync<JsonElement>();
        releases.GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task GetReleases_FiltersByPackageIds()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var package1 = new Package { Name = "pkg1", Url = "https://github.com/owner/repo1", NpmName = "pkg1", GithubOwner = "owner", GithubRepo = "repo1", CreatedAt = DateTime.UtcNow };
        var package2 = new Package { Name = "pkg2", Url = "https://github.com/owner/repo2", NpmName = "pkg2", GithubOwner = "owner", GithubRepo = "repo2", CreatedAt = DateTime.UtcNow };
        db.Packages.AddRange(package1, package2);
        await db.SaveChangesAsync();

        db.Releases.AddRange(
            new Release { PackageId = package1.Id, Tag = "v1.0.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow },
            new Release { PackageId = package2.Id, Tag = "v2.0.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/releases?packages={package1.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var releases = await response.Content.ReadFromJsonAsync<JsonElement>();
        releases.GetArrayLength().Should().Be(1);
        releases[0].GetProperty("tag").GetString().Should().Be("v1.0.0");
    }

    [Fact]
    public async Task GetReleases_FiltersByMultiplePackageIds()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var package1 = new Package { Name = "pkg1", Url = "https://github.com/owner/repo1", NpmName = "pkg1", GithubOwner = "owner", GithubRepo = "repo1", CreatedAt = DateTime.UtcNow };
        var package2 = new Package { Name = "pkg2", Url = "https://github.com/owner/repo2", NpmName = "pkg2", GithubOwner = "owner", GithubRepo = "repo2", CreatedAt = DateTime.UtcNow };
        var package3 = new Package { Name = "pkg3", Url = "https://github.com/owner/repo3", NpmName = "pkg3", GithubOwner = "owner", GithubRepo = "repo3", CreatedAt = DateTime.UtcNow };
        db.Packages.AddRange(package1, package2, package3);
        await db.SaveChangesAsync();

        db.Releases.AddRange(
            new Release { PackageId = package1.Id, Tag = "v1.0.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow },
            new Release { PackageId = package2.Id, Tag = "v2.0.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow },
            new Release { PackageId = package3.Id, Tag = "v3.0.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/releases?packages={package1.Id},{package2.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var releases = await response.Content.ReadFromJsonAsync<JsonElement>();
        releases.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetReleases_ReturnsOrderedByPublishedAtDescending()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var package = new Package { Name = "test-pkg", Url = "https://github.com/owner/repo", NpmName = "test-pkg", GithubOwner = "owner", GithubRepo = "repo", CreatedAt = DateTime.UtcNow };
        db.Packages.Add(package);
        await db.SaveChangesAsync();

        db.Releases.AddRange(
            new Release { PackageId = package.Id, Tag = "v1.0.0", PublishedAt = DateTime.UtcNow.AddDays(-5), FetchedAt = DateTime.UtcNow },
            new Release { PackageId = package.Id, Tag = "v2.0.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow },
            new Release { PackageId = package.Id, Tag = "v1.5.0", PublishedAt = DateTime.UtcNow.AddDays(-3), FetchedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/releases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var releases = await response.Content.ReadFromJsonAsync<JsonElement>();
        releases.GetArrayLength().Should().Be(3);
        releases[0].GetProperty("tag").GetString().Should().Be("v2.0.0");
        releases[1].GetProperty("tag").GetString().Should().Be("v1.5.0");
        releases[2].GetProperty("tag").GetString().Should().Be("v1.0.0");
    }

    [Fact]
    public async Task GetReleases_IncludesPackageInfo()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var package = new Package { Name = "my-package", Url = "https://github.com/my-owner/my-repo", NpmName = "my-package", GithubOwner = "my-owner", GithubRepo = "my-repo", CreatedAt = DateTime.UtcNow };
        db.Packages.Add(package);
        await db.SaveChangesAsync();

        db.Releases.Add(new Release
        {
            PackageId = package.Id,
            Tag = "v1.0.0",
            Title = "First Release",
            Body = "Release notes here",
            PublishedAt = DateTime.UtcNow.AddDays(-1),
            FetchedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/releases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var releases = await response.Content.ReadFromJsonAsync<JsonElement>();
        var release = releases[0];
        release.GetProperty("tag").GetString().Should().Be("v1.0.0");
        release.GetProperty("title").GetString().Should().Be("First Release");
        release.GetProperty("body").GetString().Should().Be("Release notes here");

        var pkg = release.GetProperty("package");
        pkg.GetProperty("npmName").GetString().Should().Be("my-package");
        pkg.GetProperty("githubOwner").GetString().Should().Be("my-owner");
        pkg.GetProperty("githubRepo").GetString().Should().Be("my-repo");
    }
}

public class ReleasesWatchlistFilterTests : IAsyncLifetime
{
    private PatchNotesApiFixture _fixture = null!;
    private HttpClient _client = null!;
    private HttpClient _authClient = null!;

    public async Task InitializeAsync()
    {
        _fixture = new PatchNotesApiFixture();
        // Configure default watchlist to known values for testing
        _fixture.ConfigureSettings(builder =>
        {
            builder.UseSetting("DefaultWatchlist:Packages:0", "default-owner/default-repo");
        });
        await _fixture.InitializeAsync();
        _client = _fixture.CreateClient();
        _authClient = _fixture.CreateAuthenticatedClient();

        // Seed user for auth tests
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        db.Users.Add(new User
        {
            StytchUserId = PatchNotesApiFixture.TestUserId,
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _authClient.Dispose();
        _client.Dispose();
        await _fixture.DisposeAsync();
        _fixture.Dispose();
    }

    [Fact]
    public async Task GetReleases_Anonymous_NoPackagesParam_FiltersToDefaultWatchlist()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();

        var defaultPkg = new Package { Name = "default-pkg", Url = "https://github.com/default-owner/default-repo", GithubOwner = "default-owner", GithubRepo = "default-repo", CreatedAt = DateTime.UtcNow };
        var otherPkg = new Package { Name = "other-pkg", Url = "https://github.com/other/repo", GithubOwner = "other", GithubRepo = "repo", CreatedAt = DateTime.UtcNow };
        db.Packages.AddRange(defaultPkg, otherPkg);
        await db.SaveChangesAsync();

        db.Releases.AddRange(
            new Release { PackageId = defaultPkg.Id, Tag = "v1.0.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow },
            new Release { PackageId = otherPkg.Id, Tag = "v2.0.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/releases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var releases = await response.Content.ReadFromJsonAsync<JsonElement>();
        releases.GetArrayLength().Should().Be(1);
        releases[0].GetProperty("tag").GetString().Should().Be("v1.0.0");
    }

    [Fact]
    public async Task GetReleases_Anonymous_WithPackagesParam_UsesExplicitFilter()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();

        var defaultPkg = new Package { Name = "default-pkg", Url = "https://github.com/default-owner/default-repo", GithubOwner = "default-owner", GithubRepo = "default-repo", CreatedAt = DateTime.UtcNow };
        var otherPkg = new Package { Name = "other-pkg", Url = "https://github.com/other/repo", GithubOwner = "other", GithubRepo = "repo", CreatedAt = DateTime.UtcNow };
        db.Packages.AddRange(defaultPkg, otherPkg);
        await db.SaveChangesAsync();

        db.Releases.AddRange(
            new Release { PackageId = defaultPkg.Id, Tag = "v1.0.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow },
            new Release { PackageId = otherPkg.Id, Tag = "v2.0.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        // Act - explicit packages param overrides default watchlist
        var response = await _client.GetAsync($"/api/releases?packages={otherPkg.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var releases = await response.Content.ReadFromJsonAsync<JsonElement>();
        releases.GetArrayLength().Should().Be(1);
        releases[0].GetProperty("tag").GetString().Should().Be("v2.0.0");
    }

    [Fact]
    public async Task GetReleases_Authenticated_WithWatchlist_FiltersToUserWatchlist()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();

        var user = await db.Users.FirstAsync(u => u.StytchUserId == PatchNotesApiFixture.TestUserId);
        var watchedPkg = new Package { Name = "watched-pkg", Url = "https://github.com/watched/repo", GithubOwner = "watched", GithubRepo = "repo", CreatedAt = DateTime.UtcNow };
        var defaultPkg = new Package { Name = "default-pkg", Url = "https://github.com/default-owner/default-repo", GithubOwner = "default-owner", GithubRepo = "default-repo", CreatedAt = DateTime.UtcNow };
        var otherPkg = new Package { Name = "other-pkg", Url = "https://github.com/other/repo", GithubOwner = "other", GithubRepo = "repo", CreatedAt = DateTime.UtcNow };
        db.Packages.AddRange(watchedPkg, defaultPkg, otherPkg);
        await db.SaveChangesAsync();

        db.Watchlists.Add(new Watchlist { UserId = user.Id, PackageId = watchedPkg.Id, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        db.Releases.AddRange(
            new Release { PackageId = watchedPkg.Id, Tag = "v1.0.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow },
            new Release { PackageId = defaultPkg.Id, Tag = "v2.0.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow },
            new Release { PackageId = otherPkg.Id, Tag = "v3.0.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        // Act
        var response = await _authClient.GetAsync("/api/releases");

        // Assert - should only return watched package releases, not default or other
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var releases = await response.Content.ReadFromJsonAsync<JsonElement>();
        releases.GetArrayLength().Should().Be(1);
        releases[0].GetProperty("tag").GetString().Should().Be("v1.0.0");
    }

    [Fact]
    public async Task GetReleases_Authenticated_EmptyWatchlist_FallsBackToDefault()
    {
        // Arrange - user has no watchlist entries
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();

        var defaultPkg = new Package { Name = "default-pkg", Url = "https://github.com/default-owner/default-repo", GithubOwner = "default-owner", GithubRepo = "default-repo", CreatedAt = DateTime.UtcNow };
        var otherPkg = new Package { Name = "other-pkg", Url = "https://github.com/other/repo", GithubOwner = "other", GithubRepo = "repo", CreatedAt = DateTime.UtcNow };
        db.Packages.AddRange(defaultPkg, otherPkg);
        await db.SaveChangesAsync();

        db.Releases.AddRange(
            new Release { PackageId = defaultPkg.Id, Tag = "v1.0.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow },
            new Release { PackageId = otherPkg.Id, Tag = "v2.0.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        // Act - authenticated but empty watchlist → falls back to default
        var response = await _authClient.GetAsync("/api/releases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var releases = await response.Content.ReadFromJsonAsync<JsonElement>();
        releases.GetArrayLength().Should().Be(1);
        releases[0].GetProperty("tag").GetString().Should().Be("v1.0.0");
    }

    [Fact]
    public async Task GetReleases_DefaultWatchlistPackageNotInDb_ReturnsEmpty()
    {
        // Arrange - default watchlist package not seeded in DB
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();

        var otherPkg = new Package { Name = "other-pkg", Url = "https://github.com/other/repo", GithubOwner = "other", GithubRepo = "repo", CreatedAt = DateTime.UtcNow };
        db.Packages.Add(otherPkg);
        await db.SaveChangesAsync();

        db.Releases.Add(new Release { PackageId = otherPkg.Id, Tag = "v1.0.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        // Act - default watchlist resolves to empty (no matching packages in DB)
        var response = await _client.GetAsync("/api/releases");

        // Assert - no default watchlist packages found → no filter → returns all
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var releases = await response.Content.ReadFromJsonAsync<JsonElement>();
        releases.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetReleases_Authenticated_WithWatchlistButNoReleasesInWindow_ReturnsEmpty()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();

        var user = await db.Users.FirstAsync(u => u.StytchUserId == PatchNotesApiFixture.TestUserId);
        var watchedPkg = new Package { Name = "watched-pkg", Url = "https://github.com/watched/repo", GithubOwner = "watched", GithubRepo = "repo", CreatedAt = DateTime.UtcNow };
        var defaultPkg = new Package { Name = "default-pkg", Url = "https://github.com/default-owner/default-repo", GithubOwner = "default-owner", GithubRepo = "default-repo", CreatedAt = DateTime.UtcNow };
        db.Packages.AddRange(watchedPkg, defaultPkg);
        await db.SaveChangesAsync();

        db.Watchlists.Add(new Watchlist { UserId = user.Id, PackageId = watchedPkg.Id, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        // Only default package has releases in window — user's watchlist package does not
        db.Releases.Add(new Release { PackageId = defaultPkg.Id, Tag = "v1.0.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        // Act
        var response = await _authClient.GetAsync("/api/releases");

        // Assert - user has watchlist, filters to it, but no releases → empty (NOT default)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var releases = await response.Content.ReadFromJsonAsync<JsonElement>();
        releases.GetArrayLength().Should().Be(0);
    }
}
