using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PatchNotes.Data;
using PatchNotes.Api.Routes;

namespace PatchNotes.Tests;

public class FeedApiTests : IAsyncLifetime
{
    private PatchNotesApiFixture _fixture = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _fixture = new PatchNotesApiFixture();
        // Clear default watchlist so test packages aren't filtered out
        _fixture.ConfigureServices(services =>
            services.Configure<DefaultWatchlistOptions>(o => o.Packages = []));
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
    public async Task GetFeed_LimitsReleasesToSummaryWindow()
    {
        // Arrange: create a package with releases spanning >7 days
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();

        var package = new Package
        {
            Name = "test-pkg",
            Url = "https://github.com/owner/test-pkg",
            NpmName = "test-pkg",
            GithubOwner = "owner",
            GithubRepo = "test-pkg"
        };
        db.Packages.Add(package);
        await db.SaveChangesAsync();

        var latestDate = DateTimeOffset.UtcNow;
        // Recent release (within window)
        db.Releases.Add(new Release
        {
            PackageId = package.Id, Tag = "v1.2.0", Title = "Recent",
            PublishedAt = latestDate, FetchedAt = latestDate,
            MajorVersion = 1, MinorVersion = 2, PatchVersion = 0
        });
        // Release within window (5 days ago)
        db.Releases.Add(new Release
        {
            PackageId = package.Id, Tag = "v1.1.0", Title = "Within window",
            PublishedAt = latestDate.AddDays(-5), FetchedAt = latestDate,
            MajorVersion = 1, MinorVersion = 1, PatchVersion = 0
        });
        // Old release (outside 7-day window)
        db.Releases.Add(new Release
        {
            PackageId = package.Id, Tag = "v1.0.0", Title = "Old",
            PublishedAt = latestDate.AddDays(-30), FetchedAt = latestDate,
            MajorVersion = 1, MinorVersion = 0, PatchVersion = 0
        });
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/feed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var feed = await response.Content.ReadFromJsonAsync<FeedResponseDto>();
        feed.Should().NotBeNull();
        feed!.Groups.Should().ContainSingle();

        var group = feed.Groups[0];
        // Only 2 releases within 7-day window of latest
        group.Releases.Should().HaveCount(2);
        group.Releases.Select(r => r.Tag).Should().Contain("v1.2.0");
        group.Releases.Select(r => r.Tag).Should().Contain("v1.1.0");
        group.Releases.Select(r => r.Tag).Should().NotContain("v1.0.0");
        // ReleaseCount reflects total count (3)
        group.ReleaseCount.Should().Be(3);
    }

    [Fact]
    public async Task GetFeed_FallsBackToLatestRelease_WhenWindowEmpty()
    {
        // Arrange: create releases where all are very old and >7 days apart
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();

        var package = new Package
        {
            Name = "old-pkg",
            Url = "https://github.com/owner/old-pkg",
            NpmName = "old-pkg",
            GithubOwner = "owner",
            GithubRepo = "old-pkg"
        };
        db.Packages.Add(package);
        await db.SaveChangesAsync();

        var baseDate = DateTimeOffset.UtcNow.AddDays(-100);
        // Two releases, each >7 days apart from each other
        db.Releases.Add(new Release
        {
            PackageId = package.Id, Tag = "v2.1.0", Title = "Latest old",
            PublishedAt = baseDate, FetchedAt = baseDate,
            MajorVersion = 2, MinorVersion = 1, PatchVersion = 0
        });
        db.Releases.Add(new Release
        {
            PackageId = package.Id, Tag = "v2.0.0", Title = "Older",
            PublishedAt = baseDate.AddDays(-30), FetchedAt = baseDate,
            MajorVersion = 2, MinorVersion = 0, PatchVersion = 0
        });
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/feed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var feed = await response.Content.ReadFromJsonAsync<FeedResponseDto>();
        feed.Should().NotBeNull();
        feed!.Groups.Should().ContainSingle();

        var group = feed.Groups[0];
        // Falls back to at least the latest release
        group.Releases.Should().ContainSingle();
        group.Releases[0].Tag.Should().Be("v2.1.0");
        group.ReleaseCount.Should().Be(2);
    }
}
