namespace PatchNotes.Sync;

/// <summary>
/// Combined result from the sync pipeline (producer + consumer).
/// </summary>
public record PipelineResult
{
    public int PackagesSynced { get; internal set; }
    public int ReleasesAdded { get; internal set; }
    public int SummariesGenerated { get; internal set; }
    public int GroupsSkipped { get; internal set; }
    public List<SyncError> SyncErrors { get; } = [];
    public List<SummaryGenerationError> SummaryErrors { get; } = [];

    public bool Success => SyncErrors.Count == 0 && SummaryErrors.Count == 0;
}
