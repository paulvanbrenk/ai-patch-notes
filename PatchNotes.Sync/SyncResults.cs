namespace PatchNotes.Sync;

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
