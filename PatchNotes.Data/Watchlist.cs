namespace PatchNotes.Data;

public class Watchlist
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PackageId { get; set; }
    public DateTime CreatedAt { get; set; }
    public User User { get; set; } = null!;
    public Package Package { get; set; } = null!;
}
