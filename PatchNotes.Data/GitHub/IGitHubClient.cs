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
    /// Gets notifications for the authenticated user.
    /// </summary>
    /// <param name="all">If true, show notifications marked as read.</param>
    /// <param name="participating">If true, only show notifications in which the user is directly participating.</param>
    /// <param name="since">Only show notifications updated after this time.</param>
    /// <param name="perPage">Number of notifications per page (max 50).</param>
    /// <param name="page">Page number for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of notifications.</returns>
    Task<IReadOnlyList<GitHubNotification>> GetNotificationsAsync(
        bool all = false,
        bool participating = false,
        DateTime? since = null,
        int perPage = 50,
        int page = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all notifications for the authenticated user, handling pagination automatically.
    /// </summary>
    /// <param name="all">If true, show notifications marked as read.</param>
    /// <param name="participating">If true, only show notifications in which the user is directly participating.</param>
    /// <param name="since">Only show notifications updated after this time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of notifications.</returns>
    IAsyncEnumerable<GitHubNotification> GetAllNotificationsAsync(
        bool all = false,
        bool participating = false,
        DateTime? since = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notifications for a specific repository.
    /// </summary>
    /// <param name="owner">The repository owner.</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="all">If true, show notifications marked as read.</param>
    /// <param name="participating">If true, only show notifications in which the user is directly participating.</param>
    /// <param name="since">Only show notifications updated after this time.</param>
    /// <param name="perPage">Number of notifications per page (max 50).</param>
    /// <param name="page">Page number for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of notifications for the repository.</returns>
    Task<IReadOnlyList<GitHubNotification>> GetRepositoryNotificationsAsync(
        string owner,
        string repo,
        bool all = false,
        bool participating = false,
        DateTime? since = null,
        int perPage = 50,
        int page = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    /// <param name="notificationId">The notification ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task MarkNotificationAsReadAsync(
        string notificationId,
        CancellationToken cancellationToken = default);
}
