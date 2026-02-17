namespace PatchNotes.Data;

public class Package : IHasCreatedAt
{
    public string Id { get; set; } = IdGenerator.NewId();
    public required string Name { get; set; }
    public required string Url { get; set; }
    public string? NpmName { get; set; }
    public required string GithubOwner { get; set; }
    public required string GithubRepo { get; set; }
    public string? TagPrefix { get; set; }
    public DateTimeOffset? LastFetchedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int ConsecutiveFailures { get; set; }
    public DateTimeOffset? LastFailureAt { get; set; }
    public string? LastFailureMessage { get; set; }
    public bool IsSyncDisabled { get; set; }

    public ICollection<Release> Releases { get; set; } = [];
    public ICollection<ReleaseSummary> ReleaseSummaries { get; set; } = [];
    public ICollection<Watchlist> Watchlists { get; set; } = [];
}
