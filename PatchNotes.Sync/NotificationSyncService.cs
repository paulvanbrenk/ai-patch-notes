using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PatchNotes.Data;
using PatchNotes.Data.GitHub;

namespace PatchNotes.Sync;

/// <summary>
/// Service for syncing GitHub notifications.
/// </summary>
public class NotificationSyncService
{
    private readonly PatchNotesDbContext _db;
    private readonly IGitHubClient _github;
    private readonly ILogger<NotificationSyncService> _logger;

    public NotificationSyncService(
        PatchNotesDbContext db,
        IGitHubClient github,
        ILogger<NotificationSyncService> logger)
    {
        _db = db;
        _github = github;
        _logger = logger;
    }

    /// <summary>
    /// Syncs all notifications from GitHub for the authenticated user.
    /// </summary>
    /// <param name="all">If true, include read notifications.</param>
    /// <param name="since">Only fetch notifications updated after this time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with count of notifications added/updated.</returns>
    public async Task<NotificationSyncResult> SyncNotificationsAsync(
        bool all = false,
        DateTime? since = null,
        CancellationToken cancellationToken = default)
    {
        var fetchedAt = DateTime.UtcNow;
        var added = 0;
        var updated = 0;

        _logger.LogInformation("Starting notifications sync (all={All}, since={Since})", all, since);

        // Get existing notification IDs to check for updates
        var existingNotifications = await _db.Notifications
            .ToDictionaryAsync(n => n.GitHubId, cancellationToken);

        // Build a lookup of packages by their GitHub full name (owner/repo)
        var packagesByRepo = await _db.Packages
            .ToDictionaryAsync(
                p => $"{p.GithubOwner}/{p.GithubRepo}".ToLowerInvariant(),
                cancellationToken);

        await foreach (var ghNotification in _github.GetAllNotificationsAsync(
            all,
            participating: false,
            since,
            cancellationToken))
        {
            var repoFullName = ghNotification.Repository.FullName.ToLowerInvariant();
            packagesByRepo.TryGetValue(repoFullName, out var package);

            if (existingNotifications.TryGetValue(ghNotification.Id, out var existing))
            {
                // Update existing notification
                existing.Unread = ghNotification.Unread;
                existing.UpdatedAt = ghNotification.UpdatedAt;
                existing.LastReadAt = ghNotification.LastReadAt;
                existing.SubjectTitle = ghNotification.Subject.Title;
                existing.FetchedAt = fetchedAt;
                updated++;
            }
            else
            {
                // Create new notification
                var notification = new Notification
                {
                    GitHubId = ghNotification.Id,
                    PackageId = package?.Id,
                    Reason = ghNotification.Reason,
                    SubjectTitle = ghNotification.Subject.Title,
                    SubjectType = ghNotification.Subject.Type,
                    SubjectUrl = ghNotification.Subject.Url,
                    RepositoryFullName = ghNotification.Repository.FullName,
                    Unread = ghNotification.Unread,
                    UpdatedAt = ghNotification.UpdatedAt,
                    LastReadAt = ghNotification.LastReadAt,
                    FetchedAt = fetchedAt
                };

                _db.Notifications.Add(notification);
                existingNotifications[ghNotification.Id] = notification;
                added++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notifications sync complete: {Added} added, {Updated} updated",
            added,
            updated);

        return new NotificationSyncResult(added, updated);
    }
}
