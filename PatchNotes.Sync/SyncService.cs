using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PatchNotes.Data;
using PatchNotes.Data.GitHub;

namespace PatchNotes.Sync;

/// <summary>
/// Service for syncing package releases from GitHub.
/// Designed to be reusable in both console app and Azure Functions.
/// </summary>
public class SyncService
{
    private readonly PatchNotesDbContext _db;
    private readonly IGitHubClient _github;
    private readonly ILogger<SyncService> _logger;

    public SyncService(
        PatchNotesDbContext db,
        IGitHubClient github,
        ILogger<SyncService> logger)
    {
        _db = db;
        _github = github;
        _logger = logger;
    }

    /// <summary>
    /// Syncs all tracked packages, fetching new releases from GitHub.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success/failure and statistics.</returns>
    public async Task<SyncResult> SyncAllAsync(CancellationToken cancellationToken = default)
    {
        var result = new SyncResult();
        var packages = await _db.Packages.ToListAsync(cancellationToken);

        _logger.LogInformation("Starting sync for {Count} packages", packages.Count);

        foreach (var package in packages)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Sync cancelled");
                break;
            }

            try
            {
                var packageResult = await SyncPackageAsync(package, cancellationToken);
                result.PackagesSynced++;
                result.ReleasesAdded += packageResult.ReleasesAdded;

                if (packageResult.ReleasesAdded > 0)
                {
                    _logger.LogInformation(
                        "Synced {Package}: {Count} new releases",
                        package.NpmName,
                        packageResult.ReleasesAdded);
                }
                else
                {
                    _logger.LogDebug("Synced {Package}: no new releases", package.NpmName);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new SyncError(package.NpmName, ex.Message));
                _logger.LogError(ex, "Failed to sync {Package}", package.NpmName);
            }
        }

        _logger.LogInformation(
            "Sync complete: {Packages} packages, {Releases} new releases, {Errors} errors",
            result.PackagesSynced,
            result.ReleasesAdded,
            result.Errors.Count);

        return result;
    }

    /// <summary>
    /// Syncs a single package, fetching new releases from GitHub.
    /// </summary>
    /// <param name="package">The package to sync.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with count of releases added.</returns>
    public async Task<PackageSyncResult> SyncPackageAsync(
        Package package,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(package.GithubOwner) || string.IsNullOrEmpty(package.GithubRepo))
        {
            _logger.LogWarning(
                "Skipping {Package}: missing GitHub owner/repo",
                package.NpmName);
            return new PackageSyncResult(0);
        }

        var since = package.LastFetchedAt;
        var fetchedAt = DateTime.UtcNow;
        var releasesAdded = 0;

        // Get existing release tags to avoid duplicates
        var existingTags = await _db.Releases
            .Where(r => r.PackageId == package.Id)
            .Select(r => r.Tag)
            .ToHashSetAsync(cancellationToken);

        await foreach (var ghRelease in _github.GetAllReleasesAsync(
            package.GithubOwner,
            package.GithubRepo,
            cancellationToken))
        {
            // Skip drafts
            if (ghRelease.Draft)
                continue;

            // Skip releases without a published date
            if (!ghRelease.PublishedAt.HasValue)
                continue;

            // If we have a last fetched date, skip older releases
            // GitHub returns releases newest-first, so once we hit an old one, we can stop
            if (since.HasValue && ghRelease.PublishedAt.Value <= since.Value)
                break;

            // Skip if we already have this release
            if (existingTags.Contains(ghRelease.TagName))
                continue;

            var release = new Release
            {
                PackageId = package.Id,
                Tag = ghRelease.TagName,
                Title = ghRelease.Name,
                Body = ghRelease.Body,
                PublishedAt = ghRelease.PublishedAt.Value,
                FetchedAt = fetchedAt
            };

            _db.Releases.Add(release);
            existingTags.Add(ghRelease.TagName);
            releasesAdded++;
        }

        package.LastFetchedAt = fetchedAt;
        await _db.SaveChangesAsync(cancellationToken);

        return new PackageSyncResult(releasesAdded);
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

/// <summary>
/// Result of syncing all packages.
/// </summary>
public record SyncResult
{
    public int PackagesSynced { get; set; }
    public int ReleasesAdded { get; set; }
    public List<SyncError> Errors { get; } = [];

    public bool Success => Errors.Count == 0;
}

/// <summary>
/// Result of syncing a single package.
/// </summary>
public record PackageSyncResult(int ReleasesAdded);

/// <summary>
/// Error that occurred during sync.
/// </summary>
public record SyncError(string PackageName, string Message);

/// <summary>
/// Result of syncing notifications.
/// </summary>
public record NotificationSyncResult(int Added, int Updated);
