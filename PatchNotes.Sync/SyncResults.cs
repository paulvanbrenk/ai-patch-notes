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
public record PackageSyncResult
{
    public int ReleasesAdded { get; init; }

    public PackageSyncResult(int releasesAdded) => ReleasesAdded = releasesAdded;
}

/// <summary>
/// Error that occurred during sync.
/// </summary>
public record SyncError(string PackageName, string Message);
