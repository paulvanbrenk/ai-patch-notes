namespace PatchNotes.Data;

public class ProcessedWebhookEvent
{
    public required string EventId { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}
