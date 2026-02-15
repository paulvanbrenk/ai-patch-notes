namespace PatchNotes.Data;

public class EmailTemplate : IHasUpdatedAt
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string Subject { get; set; } = "";
    public string JsxSource { get; set; } = "";
    public DateTimeOffset UpdatedAt { get; set; }
}
