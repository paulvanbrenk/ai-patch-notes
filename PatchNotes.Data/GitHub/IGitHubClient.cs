using PatchNotes.Data.GitHub.Models;

namespace PatchNotes.Data.GitHub;

/// <summary>
/// Client for interacting with the GitHub API.
/// </summary>
public interface IGitHubClient
{
    /// <summary>
    /// Gets all releases for a repository.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="perPage">Number of releases per page (max 100).</param>
    /// <param name="page">Page number for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of releases.</returns>
    Task<IReadOnlyList<GitHubRelease>> GetReleasesAsync(
        string owner,
        string repo,
        int perPage = 30,
        int page = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all releases for a repository, handling pagination automatically.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of releases.</returns>
    IAsyncEnumerable<GitHubRelease> GetAllReleasesAsync(
        string owner,
        string repo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the raw content of a file from a GitHub repository.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="path">The file path within the repository.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file content as a string, or null if the file doesn't exist or is too large.</returns>
    Task<string?> GetFileContentAsync(
        string owner,
        string repo,
        string path,
        CancellationToken cancellationToken = default);
}
