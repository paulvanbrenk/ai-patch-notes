namespace PatchNotes.Data;

public class ProcessedWebhookEvent
{
    public string EventId { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; }
}
