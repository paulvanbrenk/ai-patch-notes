namespace PatchNotes.Data;

public static class SummaryConstants
{
    /// <summary>
    /// Maximum time window of releases to include in a single summary.
    /// Used by both summary generation and feed display to ensure consistency.
    /// </summary>
    public static readonly TimeSpan SummaryWindow = TimeSpan.FromDays(7);
}
