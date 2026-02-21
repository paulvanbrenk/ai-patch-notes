namespace PatchNotes.Data;

public class ReleaseSummary : IHasCreatedAt, IHasUpdatedAt
{
    public string Id { get; set; } = IdGenerator.NewId();
    public required string PackageId { get; set; }
    public int MajorVersion { get; set; }
    public bool IsPrerelease { get; set; }
    public string Summary { get; set; } = null!;
    public DateTimeOffset GeneratedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Package Package { get; set; } = null!;
}
