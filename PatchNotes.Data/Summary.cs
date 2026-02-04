namespace PatchNotes.Data;

public class Summary
{
    public string Id { get; set; } = NanoidDotNet.Nanoid.Generate();
    public string PackageId { get; set; } = null!;
    public required string VersionGroup { get; set; }
    public SummaryPeriod Period { get; set; }
    public DateTime PeriodStart { get; set; }
    public required string Content { get; set; }
    public DateTime GeneratedAt { get; set; }

    public Package Package { get; set; } = null!;
}
