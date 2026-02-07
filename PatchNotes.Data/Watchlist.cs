namespace PatchNotes.Data;

public class Watchlist
{
    public string Id { get; set; } = IdGenerator.NewId();
    public string UserId { get; set; } = null!;
    public string PackageId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public User User { get; set; } = null!;
    public Package Package { get; set; } = null!;
}
