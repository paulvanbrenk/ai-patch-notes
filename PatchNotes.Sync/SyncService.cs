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
                        package.Name,
                        packageResult.ReleasesAdded);
                }
                else
                {
                    _logger.LogDebug("Synced {Package}: no new releases", package.Name);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new SyncError(package.Name, ex.Message));
                _logger.LogError(ex, "Failed to sync {Package}", package.Name);
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
    public async Task<PackageSyncResult> SyncPackageAsync(
        Package package,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(package.GithubOwner) || string.IsNullOrEmpty(package.GithubRepo))
        {
            _logger.LogWarning(
                "Skipping {Package}: missing GitHub owner/repo",
                package.Name);
            return new PackageSyncResult(0);
        }

        var since = package.LastFetchedAt;
        var fetchedAt = DateTime.UtcNow;
        var releasesAdded = 0;

        // Get existing release versions to avoid duplicates
        var existingVersions = await _db.Releases
            .Where(r => r.PackageId == package.Id)
            .Select(r => r.Version)
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
            if (existingVersions.Contains(ghRelease.TagName))
                continue;

            var (major, minor, isPrerelease) = DbSeeder.ParseVersion(ghRelease.TagName);

            var release = new Release
            {
                PackageId = package.Id,
                Version = ghRelease.TagName,
                Title = ghRelease.Name,
                Body = ghRelease.Body,
                PublishedAt = ghRelease.PublishedAt.Value,
                FetchedAt = fetchedAt,
                Major = major,
                Minor = minor,
                IsPrerelease = isPrerelease
            };

            _db.Releases.Add(release);
            existingVersions.Add(ghRelease.TagName);
            releasesAdded++;
        }

        package.LastFetchedAt = fetchedAt;
        await _db.SaveChangesAsync(cancellationToken);

        return new PackageSyncResult(releasesAdded);
    }
}
