namespace PatchNotes.Data;

public class Release
{
    public string Id { get; set; } = IdGenerator.NewId();
    public string PackageId { get; set; } = null!;
    public required string Tag { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime FetchedAt { get; set; }

    /// <summary>
    /// Parsed major version from Tag. -1 for non-semver tags (unversioned).
    /// </summary>
    public int MajorVersion { get; set; }

    /// <summary>
    /// Parsed minor version from Tag. 0 for non-semver tags.
    /// </summary>
    public int MinorVersion { get; set; }

    /// <summary>
    /// Parsed patch version from Tag. 0 for non-semver tags.
    /// </summary>
    public int PatchVersion { get; set; }

    /// <summary>
    /// Whether the Tag represents a pre-release version.
    /// </summary>
    public bool IsPrerelease { get; set; }

    /// <summary>
    /// AI-generated summary of this release. Null if not yet generated.
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// When the summary was generated. Null if no summary exists.
    /// </summary>
    public DateTime? SummaryGeneratedAt { get; set; }

    /// <summary>
    /// Indicates the existing summary is stale and needs regeneration.
    /// Defaults to true so new releases are picked up for summary generation.
    /// </summary>
    public bool SummaryStale { get; set; } = true;

    /// <summary>
    /// Concurrency token for summary persistence. Updated each time the summary
    /// is saved, preventing race conditions from concurrent summarize requests.
    /// </summary>
    public string? SummaryVersion { get; set; }

    public Package Package { get; set; } = null!;

    /// <summary>
    /// Returns true if this release needs a summary generated.
    /// </summary>
    public bool NeedsSummary => Summary == null || SummaryStale;
}
