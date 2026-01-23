using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PatchNotes.Data;
using PatchNotes.Data.GitHub;
using PatchNotes.Data.GitHub.Models;
using PatchNotes.Sync;

namespace PatchNotes.Tests;

public class SyncServiceTests : IDisposable
{
    private readonly PatchNotesDbContext _db;
    private readonly Mock<IGitHubClient> _mockGitHub;
    private readonly Mock<ILogger<SyncService>> _mockLogger;
    private readonly Mock<ILogger<NotificationSyncService>> _mockNotificationLogger;
    private readonly SyncService _syncService;
    private readonly NotificationSyncService _notificationSyncService;

    public SyncServiceTests()
    {
        var options = new DbContextOptionsBuilder<PatchNotesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new PatchNotesDbContext(options);
        _mockGitHub = new Mock<IGitHubClient>();
        _mockLogger = new Mock<ILogger<SyncService>>();
        _mockNotificationLogger = new Mock<ILogger<NotificationSyncService>>();
        _syncService = new SyncService(_db, _mockGitHub.Object, _mockLogger.Object);
        _notificationSyncService = new NotificationSyncService(_db, _mockGitHub.Object, _mockNotificationLogger.Object);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    #region SyncAllAsync Tests

    [Fact]
    public async Task SyncAllAsync_WithNoPackages_ReturnsEmptyResult()
    {
        var result = await _syncService.SyncAllAsync();

        result.PackagesSynced.Should().Be(0);
        result.ReleasesAdded.Should().Be(0);
        result.Errors.Should().BeEmpty();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SyncAllAsync_WithMultiplePackages_SyncsAll()
    {
        // Arrange
        var package1 = new Package { Name = "pkg1", Url = "https://github.com/owner1/repo1", NpmName = "pkg1", GithubOwner = "owner1", GithubRepo = "repo1" };
        var package2 = new Package { Name = "pkg2", Url = "https://github.com/owner2/repo2", NpmName = "pkg2", GithubOwner = "owner2", GithubRepo = "repo2" };
        _db.Packages.AddRange(package1, package2);
        await _db.SaveChangesAsync();

        SetupGitHubReleases("owner1", "repo1", [
            CreateRelease("v1.0.0", DateTime.UtcNow)
        ]);
        SetupGitHubReleases("owner2", "repo2", [
            CreateRelease("v2.0.0", DateTime.UtcNow),
            CreateRelease("v2.1.0", DateTime.UtcNow)
        ]);

        // Act
        var result = await _syncService.SyncAllAsync();

        // Assert
        result.PackagesSynced.Should().Be(2);
        result.ReleasesAdded.Should().Be(3);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SyncAllAsync_WithFailingPackage_ContinuesAndRecordsError()
    {
        // Arrange
        var package1 = new Package { Name = "pkg1", Url = "https://github.com/owner1/repo1", NpmName = "pkg1", GithubOwner = "owner1", GithubRepo = "repo1" };
        var package2 = new Package { Name = "pkg2", Url = "https://github.com/owner2/repo2", NpmName = "pkg2", GithubOwner = "owner2", GithubRepo = "repo2" };
        _db.Packages.AddRange(package1, package2);
        await _db.SaveChangesAsync();

        _mockGitHub
            .Setup(x => x.GetAllReleasesAsync("owner1", "repo1", It.IsAny<CancellationToken>()))
            .Returns(ThrowingAsyncEnumerable<GitHubRelease>("API Error"));

        SetupGitHubReleases("owner2", "repo2", [CreateRelease("v1.0.0", DateTime.UtcNow)]);

        // Act
        var result = await _syncService.SyncAllAsync();

        // Assert
        result.PackagesSynced.Should().Be(1);
        result.ReleasesAdded.Should().Be(1);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].PackageName.Should().Be("pkg1");
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task SyncAllAsync_PassesCancellationTokenToGitHubClient()
    {
        // Arrange
        var package = new Package { Name = "pkg1", Url = "https://github.com/owner1/repo1", NpmName = "pkg1", GithubOwner = "owner1", GithubRepo = "repo1" };
        _db.Packages.Add(package);
        await _db.SaveChangesAsync();

        using var cts = new CancellationTokenSource();
        CancellationToken? capturedToken = null;

        _mockGitHub
            .Setup(x => x.GetAllReleasesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((owner, repo, ct) =>
            {
                capturedToken = ct;
                return ToAsyncEnumerable([CreateRelease("v1.0.0", DateTime.UtcNow)]);
            });

        // Act
        await _syncService.SyncAllAsync(cancellationToken: cts.Token);

        // Assert - verify the cancellation token is properly passed through
        capturedToken.Should().NotBeNull();
        capturedToken!.Value.Should().Be(cts.Token);
    }

    #endregion

    #region SyncPackageAsync Tests

    [Fact]
    public async Task SyncPackageAsync_WithMissingGitHubInfo_ReturnsZeroReleases()
    {
        // Arrange
        var package = new Package { Name = "pkg", Url = "https://github.com//repo", NpmName = "pkg", GithubOwner = "", GithubRepo = "repo" };

        // Act
        var result = await _syncService.SyncPackageAsync(package);

        // Assert
        result.ReleasesAdded.Should().Be(0);
        _mockGitHub.Verify(x => x.GetAllReleasesAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SyncPackageAsync_WithNewReleases_AddsToDatabase()
    {
        // Arrange
        var package = new Package { Name = "pkg", Url = "https://github.com/owner/repo", NpmName = "pkg", GithubOwner = "owner", GithubRepo = "repo" };
        _db.Packages.Add(package);
        await _db.SaveChangesAsync();

        var publishedAt = DateTime.UtcNow;
        SetupGitHubReleases("owner", "repo", [
            CreateRelease("v1.0.0", publishedAt, "Release 1", "Body 1"),
            CreateRelease("v1.1.0", publishedAt, "Release 2", "Body 2")
        ]);

        // Act
        var result = await _syncService.SyncPackageAsync(package);

        // Assert
        result.ReleasesAdded.Should().Be(2);

        var releases = await _db.Releases.Where(r => r.PackageId == package.Id).ToListAsync();
        releases.Should().HaveCount(2);
        releases.Should().Contain(r => r.Tag == "v1.0.0");
        releases.Should().Contain(r => r.Tag == "v1.1.0");
    }

    [Fact]
    public async Task SyncPackageAsync_SkipsDraftReleases()
    {
        // Arrange
        var package = new Package { Name = "pkg", Url = "https://github.com/owner/repo", NpmName = "pkg", GithubOwner = "owner", GithubRepo = "repo" };
        _db.Packages.Add(package);
        await _db.SaveChangesAsync();

        SetupGitHubReleases("owner", "repo", [
            CreateRelease("v1.0.0", DateTime.UtcNow, draft: true),
            CreateRelease("v1.1.0", DateTime.UtcNow, draft: false)
        ]);

        // Act
        var result = await _syncService.SyncPackageAsync(package);

        // Assert
        result.ReleasesAdded.Should().Be(1);

        var releases = await _db.Releases.Where(r => r.PackageId == package.Id).ToListAsync();
        releases.Should().ContainSingle(r => r.Tag == "v1.1.0");
    }

    [Fact]
    public async Task SyncPackageAsync_SkipsReleasesWithoutPublishedDate()
    {
        // Arrange
        var package = new Package { Name = "pkg", Url = "https://github.com/owner/repo", NpmName = "pkg", GithubOwner = "owner", GithubRepo = "repo" };
        _db.Packages.Add(package);
        await _db.SaveChangesAsync();

        SetupGitHubReleases("owner", "repo", [
            new GitHubRelease { TagName = "v1.0.0", PublishedAt = null },
            CreateRelease("v1.1.0", DateTime.UtcNow)
        ]);

        // Act
        var result = await _syncService.SyncPackageAsync(package);

        // Assert
        result.ReleasesAdded.Should().Be(1);
    }

    [Fact]
    public async Task SyncPackageAsync_SkipsDuplicateTags()
    {
        // Arrange
        var package = new Package { Name = "pkg", Url = "https://github.com/owner/repo", NpmName = "pkg", GithubOwner = "owner", GithubRepo = "repo" };
        _db.Packages.Add(package);
        await _db.SaveChangesAsync();

        // Add existing release
        _db.Releases.Add(new Release
        {
            PackageId = package.Id,
            Tag = "v1.0.0",
            PublishedAt = DateTime.UtcNow.AddDays(-1),
            FetchedAt = DateTime.UtcNow.AddDays(-1)
        });
        await _db.SaveChangesAsync();

        SetupGitHubReleases("owner", "repo", [
            CreateRelease("v1.0.0", DateTime.UtcNow),
            CreateRelease("v1.1.0", DateTime.UtcNow)
        ]);

        // Act
        var result = await _syncService.SyncPackageAsync(package);

        // Assert
        result.ReleasesAdded.Should().Be(1);

        var releases = await _db.Releases.Where(r => r.PackageId == package.Id).ToListAsync();
        releases.Should().HaveCount(2);
    }

    [Fact]
    public async Task SyncPackageAsync_StopsAtOlderReleases_WhenLastFetchedAtIsSet()
    {
        // Arrange
        var lastFetched = DateTime.UtcNow.AddDays(-1);
        var package = new Package
        {
            Name = "pkg",
            Url = "https://github.com/owner/repo",
            NpmName = "pkg",
            GithubOwner = "owner",
            GithubRepo = "repo",
            LastFetchedAt = lastFetched
        };
        _db.Packages.Add(package);
        await _db.SaveChangesAsync();

        SetupGitHubReleases("owner", "repo", [
            CreateRelease("v1.2.0", DateTime.UtcNow),
            CreateRelease("v1.1.0", lastFetched.AddHours(-1)) // Older than lastFetched - should stop here
        ]);

        // Act
        var result = await _syncService.SyncPackageAsync(package);

        // Assert
        result.ReleasesAdded.Should().Be(1);

        var releases = await _db.Releases.Where(r => r.PackageId == package.Id).ToListAsync();
        releases.Should().ContainSingle(r => r.Tag == "v1.2.0");
    }

    [Fact]
    public async Task SyncPackageAsync_UpdatesLastFetchedAt()
    {
        // Arrange
        var package = new Package { Name = "pkg", Url = "https://github.com/owner/repo", NpmName = "pkg", GithubOwner = "owner", GithubRepo = "repo" };
        _db.Packages.Add(package);
        await _db.SaveChangesAsync();

        var beforeSync = DateTime.UtcNow;
        SetupGitHubReleases("owner", "repo", []);

        // Act
        await _syncService.SyncPackageAsync(package);

        // Assert
        package.LastFetchedAt.Should().NotBeNull();
        package.LastFetchedAt.Should().BeOnOrAfter(beforeSync);
    }

    [Fact]
    public async Task SyncPackageAsync_ReturnsNewReleasesAsNeedingSummary()
    {
        // Arrange
        var package = new Package { Name = "pkg", Url = "https://github.com/owner/repo", NpmName = "pkg", GithubOwner = "owner", GithubRepo = "repo" };
        _db.Packages.Add(package);
        await _db.SaveChangesAsync();

        var publishedAt = DateTime.UtcNow;
        SetupGitHubReleases("owner", "repo", [
            CreateRelease("v1.0.0", publishedAt, "Release 1", "Body 1"),
            CreateRelease("v1.1.0", publishedAt, "Release 2", "Body 2")
        ]);

        // Act
        var result = await _syncService.SyncPackageAsync(package);

        // Assert
        result.ReleasesAdded.Should().Be(2);
        result.ReleasesNeedingSummary.Should().HaveCount(2);
        result.ReleasesNeedingSummary.Should().AllSatisfy(r => r.NeedsSummary.Should().BeTrue());
    }

    [Fact]
    public async Task SyncPackageAsync_WithIncludeExistingWithoutSummary_ReturnsAllReleasesWithoutSummary()
    {
        // Arrange
        var package = new Package { Name = "pkg", Url = "https://github.com/owner/repo", NpmName = "pkg", GithubOwner = "owner", GithubRepo = "repo" };
        _db.Packages.Add(package);
        await _db.SaveChangesAsync();

        // Add existing release without summary
        _db.Releases.Add(new Release
        {
            PackageId = package.Id,
            Tag = "v0.9.0",
            PublishedAt = DateTime.UtcNow.AddDays(-10),
            FetchedAt = DateTime.UtcNow.AddDays(-10),
            Summary = null
        });
        // Add existing release with summary
        _db.Releases.Add(new Release
        {
            PackageId = package.Id,
            Tag = "v0.8.0",
            PublishedAt = DateTime.UtcNow.AddDays(-20),
            FetchedAt = DateTime.UtcNow.AddDays(-20),
            Summary = "This is a summary",
            SummaryGeneratedAt = DateTime.UtcNow.AddDays(-19)
        });
        await _db.SaveChangesAsync();

        var publishedAt = DateTime.UtcNow;
        SetupGitHubReleases("owner", "repo", [
            CreateRelease("v1.0.0", publishedAt, "Release 1", "Body 1")
        ]);

        // Act
        var result = await _syncService.SyncPackageAsync(package, includeExistingWithoutSummary: true);

        // Assert
        result.ReleasesAdded.Should().Be(1);
        // Should include the new release + the existing one without summary (not the one with summary)
        result.ReleasesNeedingSummary.Should().HaveCount(2);
        result.ReleasesNeedingSummary.Should().Contain(r => r.Tag == "v1.0.0");
        result.ReleasesNeedingSummary.Should().Contain(r => r.Tag == "v0.9.0");
        result.ReleasesNeedingSummary.Should().NotContain(r => r.Tag == "v0.8.0");
    }

    [Fact]
    public async Task SyncAllAsync_AggregatesReleasesNeedingSummary()
    {
        // Arrange
        var package1 = new Package { Name = "pkg1", Url = "https://github.com/owner1/repo1", NpmName = "pkg1", GithubOwner = "owner1", GithubRepo = "repo1" };
        var package2 = new Package { Name = "pkg2", Url = "https://github.com/owner2/repo2", NpmName = "pkg2", GithubOwner = "owner2", GithubRepo = "repo2" };
        _db.Packages.AddRange(package1, package2);
        await _db.SaveChangesAsync();

        SetupGitHubReleases("owner1", "repo1", [CreateRelease("v1.0.0", DateTime.UtcNow)]);
        SetupGitHubReleases("owner2", "repo2", [
            CreateRelease("v2.0.0", DateTime.UtcNow),
            CreateRelease("v2.1.0", DateTime.UtcNow)
        ]);

        // Act
        var result = await _syncService.SyncAllAsync();

        // Assert
        result.PackagesSynced.Should().Be(2);
        result.ReleasesAdded.Should().Be(3);
        result.ReleasesNeedingSummary.Should().HaveCount(3);
    }

    #endregion

    #region GetReleasesNeedingSummaryAsync Tests

    [Fact]
    public async Task GetReleasesNeedingSummaryAsync_ReturnsReleasesWithoutSummary()
    {
        // Arrange
        var package = new Package { Name = "pkg", Url = "https://github.com/owner/repo", NpmName = "pkg", GithubOwner = "owner", GithubRepo = "repo" };
        _db.Packages.Add(package);
        await _db.SaveChangesAsync();

        _db.Releases.AddRange(
            new Release { PackageId = package.Id, Tag = "v1.0.0", PublishedAt = DateTime.UtcNow.AddDays(-3), FetchedAt = DateTime.UtcNow.AddDays(-3), Summary = null },
            new Release { PackageId = package.Id, Tag = "v1.1.0", PublishedAt = DateTime.UtcNow.AddDays(-2), FetchedAt = DateTime.UtcNow.AddDays(-2), Summary = "Has summary", SummaryGeneratedAt = DateTime.UtcNow.AddDays(-1) },
            new Release { PackageId = package.Id, Tag = "v1.2.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow.AddDays(-1), Summary = null }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _syncService.GetReleasesNeedingSummaryAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Tag == "v1.0.0");
        result.Should().Contain(r => r.Tag == "v1.2.0");
        result.Should().NotContain(r => r.Tag == "v1.1.0");
    }

    [Fact]
    public async Task GetReleasesNeedingSummaryAsync_ReturnsOrderedByPublishedAtDescending()
    {
        // Arrange
        var package = new Package { Name = "pkg", Url = "https://github.com/owner/repo", NpmName = "pkg", GithubOwner = "owner", GithubRepo = "repo" };
        _db.Packages.Add(package);
        await _db.SaveChangesAsync();

        _db.Releases.AddRange(
            new Release { PackageId = package.Id, Tag = "v1.0.0", PublishedAt = DateTime.UtcNow.AddDays(-3), FetchedAt = DateTime.UtcNow },
            new Release { PackageId = package.Id, Tag = "v1.1.0", PublishedAt = DateTime.UtcNow.AddDays(-1), FetchedAt = DateTime.UtcNow },
            new Release { PackageId = package.Id, Tag = "v1.2.0", PublishedAt = DateTime.UtcNow.AddDays(-2), FetchedAt = DateTime.UtcNow }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _syncService.GetReleasesNeedingSummaryAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].Tag.Should().Be("v1.1.0"); // Most recent
        result[1].Tag.Should().Be("v1.2.0");
        result[2].Tag.Should().Be("v1.0.0"); // Oldest
    }

    [Fact]
    public async Task GetReleasesNeedingSummaryAsync_ByPackageId_ReturnsOnlyForThatPackage()
    {
        // Arrange
        var package1 = new Package { Name = "pkg1", Url = "https://github.com/owner1/repo1", NpmName = "pkg1", GithubOwner = "owner1", GithubRepo = "repo1" };
        var package2 = new Package { Name = "pkg2", Url = "https://github.com/owner2/repo2", NpmName = "pkg2", GithubOwner = "owner2", GithubRepo = "repo2" };
        _db.Packages.AddRange(package1, package2);
        await _db.SaveChangesAsync();

        _db.Releases.AddRange(
            new Release { PackageId = package1.Id, Tag = "v1.0.0", PublishedAt = DateTime.UtcNow, FetchedAt = DateTime.UtcNow },
            new Release { PackageId = package1.Id, Tag = "v1.1.0", PublishedAt = DateTime.UtcNow, FetchedAt = DateTime.UtcNow },
            new Release { PackageId = package2.Id, Tag = "v2.0.0", PublishedAt = DateTime.UtcNow, FetchedAt = DateTime.UtcNow }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _syncService.GetReleasesNeedingSummaryAsync(package1.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.PackageId.Should().Be(package1.Id));
    }

    #endregion

    #region SyncNotificationsAsync Tests

    [Fact]
    public async Task SyncNotificationsAsync_WithNewNotifications_AddsToDatabase()
    {
        // Arrange
        var package = new Package { Name = "pkg", Url = "https://github.com/owner/repo", NpmName = "pkg", GithubOwner = "owner", GithubRepo = "repo" };
        _db.Packages.Add(package);
        await _db.SaveChangesAsync();

        SetupGitHubNotifications([
            CreateNotification("1", "owner/repo", "mention", "Issue Title", "Issue")
        ]);

        // Act
        var result = await _notificationSyncService.SyncNotificationsAsync();

        // Assert
        result.Added.Should().Be(1);
        result.Updated.Should().Be(0);

        var notifications = await _db.Notifications.ToListAsync();
        notifications.Should().ContainSingle();
        notifications[0].GitHubId.Should().Be("1");
        notifications[0].PackageId.Should().Be(package.Id);
    }

    [Fact]
    public async Task SyncNotificationsAsync_WithExistingNotifications_UpdatesThem()
    {
        // Arrange
        var existingNotification = new Notification
        {
            GitHubId = "1",
            Reason = "mention",
            SubjectTitle = "Old Title",
            SubjectType = "Issue",
            RepositoryFullName = "owner/repo",
            Unread = true,
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            FetchedAt = DateTime.UtcNow.AddDays(-1)
        };
        _db.Notifications.Add(existingNotification);
        await _db.SaveChangesAsync();

        SetupGitHubNotifications([
            CreateNotification("1", "owner/repo", "mention", "New Title", "Issue", unread: false)
        ]);

        // Act
        var result = await _notificationSyncService.SyncNotificationsAsync();

        // Assert
        result.Added.Should().Be(0);
        result.Updated.Should().Be(1);

        var notification = await _db.Notifications.SingleAsync();
        notification.SubjectTitle.Should().Be("New Title");
        notification.Unread.Should().BeFalse();
    }

    [Fact]
    public async Task SyncNotificationsAsync_LinksToMatchingPackage()
    {
        // Arrange
        var package = new Package { Name = "pkg", Url = "https://github.com/owner/repo", NpmName = "pkg", GithubOwner = "owner", GithubRepo = "repo" };
        _db.Packages.Add(package);
        await _db.SaveChangesAsync();

        SetupGitHubNotifications([
            CreateNotification("1", "owner/repo", "mention", "Title", "Issue"),
            CreateNotification("2", "other/repo", "mention", "Title 2", "Issue")
        ]);

        // Act
        var result = await _notificationSyncService.SyncNotificationsAsync();

        // Assert
        result.Added.Should().Be(2);

        var notifications = await _db.Notifications.ToListAsync();
        notifications.Should().HaveCount(2);
        notifications.First(n => n.GitHubId == "1").PackageId.Should().Be(package.Id);
        notifications.First(n => n.GitHubId == "2").PackageId.Should().BeNull();
    }

    [Fact]
    public async Task SyncNotificationsAsync_PassesParametersToClient()
    {
        // Arrange
        var since = DateTime.UtcNow.AddDays(-7);
        SetupGitHubNotifications([]);

        // Act
        await _notificationSyncService.SyncNotificationsAsync(all: true, since: since);

        // Assert
        _mockGitHub.Verify(x => x.GetAllNotificationsAsync(
            true,
            false,
            since,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupGitHubReleases(string owner, string repo, List<GitHubRelease> releases)
    {
        _mockGitHub
            .Setup(x => x.GetAllReleasesAsync(owner, repo, It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(releases));
    }

    private void SetupGitHubNotifications(List<GitHubNotification> notifications)
    {
        _mockGitHub
            .Setup(x => x.GetAllNotificationsAsync(
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(notifications));
    }

    private static GitHubRelease CreateRelease(
        string tag,
        DateTime publishedAt,
        string? name = null,
        string? body = null,
        bool draft = false)
    {
        return new GitHubRelease
        {
            Id = Random.Shared.NextInt64(),
            TagName = tag,
            Name = name ?? tag,
            Body = body,
            Draft = draft,
            PublishedAt = publishedAt
        };
    }

    private static GitHubNotification CreateNotification(
        string id,
        string repoFullName,
        string reason,
        string subjectTitle,
        string subjectType,
        bool unread = true)
    {
        var parts = repoFullName.Split('/');
        return new GitHubNotification
        {
            Id = id,
            Reason = reason,
            Unread = unread,
            UpdatedAt = DateTime.UtcNow,
            Subject = new GitHubNotificationSubject
            {
                Title = subjectTitle,
                Type = subjectType
            },
            Repository = new GitHubNotificationRepository
            {
                Id = Random.Shared.NextInt64(),
                Name = parts[1],
                FullName = repoFullName,
                Owner = new GitHubNotificationOwner
                {
                    Login = parts[0],
                    Id = Random.Shared.NextInt64()
                }
            }
        };
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            yield return item;
        }
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<T> ThrowingAsyncEnumerable<T>(string message)
    {
        await Task.CompletedTask;
        throw new Exception(message);
#pragma warning disable CS0162 // Unreachable code detected
        yield break; // Never reached, but required for compiler
#pragma warning restore CS0162
    }

    #endregion
}
