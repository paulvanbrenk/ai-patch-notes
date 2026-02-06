namespace PatchNotes.Data;

public class ProcessedWebhookEvent
{
    public string EventId { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}
