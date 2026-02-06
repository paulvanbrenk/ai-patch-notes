namespace PatchNotes.Data;

public class Release
{
    public int Id { get; set; }
    public int PackageId { get; set; }
    public required string Tag { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime FetchedAt { get; set; }

    /// <summary>
    /// AI-generated summary of this release. Null if not yet generated.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// When the summary was generated. Null if no summary exists.
    /// </summary>
    public DateTime? SummaryGeneratedAt { get; set; }

    public Package Package { get; set; } = null!;

    /// <summary>
    /// Returns true if this release needs a summary generated.
    /// </summary>
    public bool NeedsSummary => Summary == null;
}
