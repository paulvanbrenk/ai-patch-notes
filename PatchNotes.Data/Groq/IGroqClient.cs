namespace PatchNotes.Data.Groq;

/// <summary>
/// Interface for the Groq API client.
/// </summary>
public interface IGroqClient
{
    /// <summary>
    /// Summarizes release notes into a concise description.
    /// </summary>
    /// <param name="releaseTitle">The title of the release.</param>
    /// <param name="releaseBody">The body/content of the release notes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A summarized version of the release notes.</returns>
    Task<string> SummarizeReleaseNotesAsync(
        string? releaseTitle,
        string? releaseBody,
        CancellationToken cancellationToken = default);
}
