namespace PatchNotes.Data;

public class Release
{
    public string Id { get; set; } = IdGenerator.NewId();
    public string PackageId { get; set; } = null!;
    public required string Tag { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public DateTimeOffset FetchedAt { get; set; }

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
    /// Indicates the existing summary is stale and needs regeneration.
    /// Defaults to true so new releases are picked up for summary generation.
    /// </summary>
    public bool SummaryStale { get; set; } = true;

    public Package Package { get; set; } = null!;
}
