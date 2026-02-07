namespace PatchNotes.Data;

public class ReleaseSummary
{
    public string Id { get; set; } = IdGenerator.NewId();
    public string PackageId { get; set; } = null!;
    public int MajorVersion { get; set; }
    public bool IsPrerelease { get; set; }
    public string Summary { get; set; } = null!;
    public DateTime GeneratedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Package Package { get; set; } = null!;
}
