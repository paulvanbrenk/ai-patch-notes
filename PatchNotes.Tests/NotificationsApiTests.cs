using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PatchNotes.Data;

namespace PatchNotes.Tests;

public class NotificationsApiTests : IAsyncLifetime
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
    public async Task GetNotifications_ReturnsEmptyList_WhenNoNotifications()
    {
        var response = await _client.GetAsync("/api/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notifications = await response.Content.ReadFromJsonAsync<JsonElement>();
        notifications.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetNotifications_ReturnsAllNotifications()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        db.Notifications.AddRange(
            CreateNotification("1", true),
            CreateNotification("2", false),
            CreateNotification("3", true)
        );
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notifications = await response.Content.ReadFromJsonAsync<JsonElement>();
        notifications.GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task GetNotifications_FiltersUnreadOnly()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        db.Notifications.AddRange(
            CreateNotification("1", unread: true),
            CreateNotification("2", unread: false),
            CreateNotification("3", unread: true)
        );
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/notifications?unreadOnly=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notifications = await response.Content.ReadFromJsonAsync<JsonElement>();
        notifications.GetArrayLength().Should().Be(2);
        foreach (var notification in notifications.EnumerateArray())
        {
            notification.GetProperty("unread").GetBoolean().Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetNotifications_FiltersByPackageId()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();

        var package = new Package { NpmName = "test-pkg", GithubOwner = "owner", GithubRepo = "repo", CreatedAt = DateTime.UtcNow };
        db.Packages.Add(package);
        await db.SaveChangesAsync();

        db.Notifications.AddRange(
            CreateNotification("1", packageId: package.Id),
            CreateNotification("2", packageId: null),
            CreateNotification("3", packageId: package.Id)
        );
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/notifications?packageId={package.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notifications = await response.Content.ReadFromJsonAsync<JsonElement>();
        notifications.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetNotifications_ReturnsOrderedByUpdatedAtDescending()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        db.Notifications.AddRange(
            CreateNotification("oldest", updatedAt: DateTime.UtcNow.AddDays(-5)),
            CreateNotification("newest", updatedAt: DateTime.UtcNow.AddDays(-1)),
            CreateNotification("middle", updatedAt: DateTime.UtcNow.AddDays(-3))
        );
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notifications = await response.Content.ReadFromJsonAsync<JsonElement>();
        notifications.GetArrayLength().Should().Be(3);
        notifications[0].GetProperty("gitHubId").GetString().Should().Be("newest");
        notifications[1].GetProperty("gitHubId").GetString().Should().Be("middle");
        notifications[2].GetProperty("gitHubId").GetString().Should().Be("oldest");
    }

    [Fact]
    public async Task GetNotifications_IncludesPackageInfo_WhenAvailable()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();

        var package = new Package { NpmName = "my-pkg", GithubOwner = "owner", GithubRepo = "repo", CreatedAt = DateTime.UtcNow };
        db.Packages.Add(package);
        await db.SaveChangesAsync();

        db.Notifications.Add(CreateNotification("1", packageId: package.Id));
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notifications = await response.Content.ReadFromJsonAsync<JsonElement>();
        var notification = notifications[0];
        var pkg = notification.GetProperty("package");
        pkg.GetProperty("npmName").GetString().Should().Be("my-pkg");
        pkg.GetProperty("githubOwner").GetString().Should().Be("owner");
        pkg.GetProperty("githubRepo").GetString().Should().Be("repo");
    }

    [Fact]
    public async Task GetNotifications_PackageIsNull_WhenNoPackageLinked()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        db.Notifications.Add(CreateNotification("1", packageId: null));
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notifications = await response.Content.ReadFromJsonAsync<JsonElement>();
        var notification = notifications[0];
        notification.GetProperty("package").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetNotifications_ReturnsCorrectFields()
    {
        // Arrange
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var notification = new Notification
        {
            GitHubId = "gh-123",
            Reason = "review_requested",
            SubjectTitle = "Fix bug #42",
            SubjectType = "PullRequest",
            SubjectUrl = "https://api.github.com/repos/owner/repo/pulls/42",
            RepositoryFullName = "owner/repo",
            Unread = true,
            UpdatedAt = DateTime.UtcNow,
            LastReadAt = DateTime.UtcNow.AddHours(-1),
            FetchedAt = DateTime.UtcNow
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var notifications = await response.Content.ReadFromJsonAsync<JsonElement>();
        var n = notifications[0];
        n.GetProperty("gitHubId").GetString().Should().Be("gh-123");
        n.GetProperty("reason").GetString().Should().Be("review_requested");
        n.GetProperty("subjectTitle").GetString().Should().Be("Fix bug #42");
        n.GetProperty("subjectType").GetString().Should().Be("PullRequest");
        n.GetProperty("subjectUrl").GetString().Should().Be("https://api.github.com/repos/owner/repo/pulls/42");
        n.GetProperty("repositoryFullName").GetString().Should().Be("owner/repo");
        n.GetProperty("unread").GetBoolean().Should().BeTrue();
    }

    private static Notification CreateNotification(
        string gitHubId,
        bool unread = true,
        int? packageId = null,
        DateTime? updatedAt = null)
    {
        return new Notification
        {
            GitHubId = gitHubId,
            Reason = "mention",
            SubjectTitle = $"Subject {gitHubId}",
            SubjectType = "Issue",
            RepositoryFullName = "owner/repo",
            Unread = unread,
            PackageId = packageId,
            UpdatedAt = updatedAt ?? DateTime.UtcNow,
            FetchedAt = DateTime.UtcNow
        };
    }
}
