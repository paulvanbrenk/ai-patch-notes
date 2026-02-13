namespace PatchNotes.Data;

public class Watchlist : IHasCreatedAt
{
    public string Id { get; set; } = IdGenerator.NewId();
    public string UserId { get; set; } = null!;
    public string PackageId { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public User User { get; set; } = null!;
    public Package Package { get; set; } = null!;
}
