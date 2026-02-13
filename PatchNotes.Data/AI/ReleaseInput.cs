namespace PatchNotes.Data.AI;

/// <summary>
/// Represents a single release to be summarized by the AI client.
/// </summary>
public record ReleaseInput(string Tag, string? Title, string? Body, DateTimeOffset? PublishedAt = null);
