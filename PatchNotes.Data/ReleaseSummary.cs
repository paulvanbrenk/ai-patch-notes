namespace PatchNotes.Data;

public class ReleaseSummary : IHasUpdatedAt
{
    public string Id { get; set; } = IdGenerator.NewId();
    public string PackageId { get; set; } = null!;
    public int MajorVersion { get; set; }
    public bool IsPrerelease { get; set; }
    public string Summary { get; set; } = null!;
    public DateTimeOffset GeneratedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Package Package { get; set; } = null!;
}
