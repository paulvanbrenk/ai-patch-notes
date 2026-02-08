namespace PatchNotes.Data.AI;

/// <summary>
/// Interface for the AI client.
/// </summary>
public interface IAiClient
{
    /// <summary>
    /// Summarizes one or more release changelogs into a concise description.
    /// </summary>
    /// <param name="packageName">The name of the package (e.g. "react").</param>
    /// <param name="releases">One or more releases to summarize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A summarized version of the release notes.</returns>
    Task<string> SummarizeReleaseNotesAsync(
        string packageName,
        IReadOnlyList<ReleaseInput> releases,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Summarizes one or more release changelogs with streaming support.
    /// </summary>
    /// <param name="packageName">The name of the package (e.g. "react").</param>
    /// <param name="releases">One or more releases to summarize.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of summary chunks.</returns>
    IAsyncEnumerable<string> SummarizeReleaseNotesStreamAsync(
        string packageName,
        IReadOnlyList<ReleaseInput> releases,
        CancellationToken cancellationToken = default);
}
