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

    public Package Package { get; set; } = null!;
}
