namespace PatchNotes.Data;

public class Release
{
    public string Id { get; set; } = NanoidDotNet.Nanoid.Generate();
    public string PackageId { get; set; } = null!;
    public required string Version { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateTime FetchedAt { get; set; }
    public int Major { get; set; }
    public int Minor { get; set; }
    public bool IsPrerelease { get; set; }

    public Package Package { get; set; } = null!;
}
