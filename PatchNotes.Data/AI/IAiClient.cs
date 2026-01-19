namespace PatchNotes.Data.AI;

/// <summary>
/// Interface for the AI client.
/// </summary>
public interface IAiClient
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

    /// <summary>
    /// Summarizes release notes with streaming support.
    /// </summary>
    /// <param name="releaseTitle">The title of the release.</param>
    /// <param name="releaseBody">The body/content of the release notes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of summary chunks.</returns>
    IAsyncEnumerable<string> SummarizeReleaseNotesStreamAsync(
        string? releaseTitle,
        string? releaseBody,
        CancellationToken cancellationToken = default);
}
