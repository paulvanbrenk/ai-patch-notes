using PatchNotes.Data;

namespace PatchNotes.Sync;

/// <summary>
/// Result of syncing all packages.
/// </summary>
public record SyncResult
{
    public int PackagesSynced { get; set; }
    public int ReleasesAdded { get; set; }
    public List<SyncError> Errors { get; } = [];

    /// <summary>
    /// Releases that need summary generation (new releases or releases without summaries).
    /// </summary>
    public List<Release> ReleasesNeedingSummary { get; } = [];

    public bool Success => Errors.Count == 0;
}

/// <summary>
/// Result of syncing a single package.
/// </summary>
public record PackageSyncResult
{
    public int ReleasesAdded { get; init; }

    /// <summary>
    /// Releases from this package that need summary generation.
    /// </summary>
    public List<Release> ReleasesNeedingSummary { get; init; } = [];

    public PackageSyncResult(int releasesAdded) => ReleasesAdded = releasesAdded;

    public PackageSyncResult(int releasesAdded, List<Release> releasesNeedingSummary)
    {
        ReleasesAdded = releasesAdded;
        ReleasesNeedingSummary = releasesNeedingSummary;
    }
}

/// <summary>
/// Error that occurred during sync.
/// </summary>
public record SyncError(string PackageName, string Message);

/// <summary>
/// Result of syncing notifications.
/// </summary>
public record NotificationSyncResult(int Added, int Updated);
