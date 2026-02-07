namespace PatchNotes.Data;

public class Package
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Url { get; set; }
    public string? NpmName { get; set; }
    public required string GithubOwner { get; set; }
    public required string GithubRepo { get; set; }
    public DateTime? LastFetchedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Release> Releases { get; set; } = [];
    public ICollection<Watchlist> Watchlists { get; set; } = [];
}
