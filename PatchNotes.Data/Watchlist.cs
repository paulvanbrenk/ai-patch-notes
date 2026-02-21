namespace PatchNotes.Data;

public class Watchlist : IHasCreatedAt
{
    public string Id { get; set; } = IdGenerator.NewId();
    public required string UserId { get; set; }
    public required string PackageId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public User User { get; set; } = null!;
    public Package Package { get; set; } = null!;
}
