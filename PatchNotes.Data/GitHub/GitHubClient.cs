using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace PatchNotes.Data.GitHub;

/// <summary>
/// Client for interacting with the GitHub API.
/// </summary>
public class GitHubClient : IGitHubClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubClient> _logger;

    public GitHubClient(HttpClient httpClient, ILogger<GitHubClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GitHubRelease>> GetReleasesAsync(
        string owner,
        string repo,
        int perPage = 30,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentOutOfRangeException.ThrowIfLessThan(perPage, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(perPage, 100);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);

        var url = $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/releases?per_page={perPage}&page={page}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var rateLimitInfo = ParseRateLimitHeaders(response);
        LogRateLimitStatus(rateLimitInfo, owner, repo);

        var releases = await response.Content.ReadFromJsonAsync<List<GitHubRelease>>(cancellationToken);

        return releases ?? [];
    }

    private GitHubRateLimitInfo ParseRateLimitHeaders(HttpResponseMessage response)
    {
        var headers = response.Headers;

        int.TryParse(GetHeaderValue(headers, "X-RateLimit-Limit"), out var limit);
        int.TryParse(GetHeaderValue(headers, "X-RateLimit-Remaining"), out var remaining);
        int.TryParse(GetHeaderValue(headers, "X-RateLimit-Used"), out var used);
        long.TryParse(GetHeaderValue(headers, "X-RateLimit-Reset"), out var resetUnix);

        var resetAt = resetUnix > 0
            ? DateTimeOffset.FromUnixTimeSeconds(resetUnix)
            : DateTimeOffset.MinValue;

        return new GitHubRateLimitInfo
        {
            Limit = limit,
            Remaining = remaining,
            Used = used,
            ResetAt = resetAt
        };
    }

    private static string? GetHeaderValue(System.Net.Http.Headers.HttpResponseHeaders headers, string name) =>
        headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

    private void LogRateLimitStatus(GitHubRateLimitInfo rateLimitInfo, string owner, string repo)
    {
        if (!rateLimitInfo.IsValid)
        {
            return;
        }

        if (rateLimitInfo.IsApproachingLimit(10))
        {
            _logger.LogWarning(
                "GitHub API rate limit approaching for {Owner}/{Repo}: {Remaining}/{Limit} requests remaining ({Percentage:F1}%). Resets at {ResetAt:u}",
                owner,
                repo,
                rateLimitInfo.Remaining,
                rateLimitInfo.Limit,
                rateLimitInfo.RemainingPercentage,
                rateLimitInfo.ResetAt);
        }
        else
        {
            _logger.LogDebug(
                "GitHub API rate limit status: {Remaining}/{Limit} requests remaining. Resets at {ResetAt:u}",
                rateLimitInfo.Remaining,
                rateLimitInfo.Limit,
                rateLimitInfo.ResetAt);
        }
    }

    public async IAsyncEnumerable<GitHubRelease> GetAllReleasesAsync(
        string owner,
        string repo,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const int perPage = 100;
        var page = 1;

        while (!cancellationToken.IsCancellationRequested)
        {
            var releases = await GetReleasesAsync(owner, repo, perPage, page, cancellationToken);

            if (releases.Count == 0)
            {
                yield break;
            }

            foreach (var release in releases)
            {
                yield return release;
            }

            if (releases.Count < perPage)
            {
                yield break;
            }

            page++;
        }
    }

    public async Task<IReadOnlyList<GitHubNotification>> GetNotificationsAsync(
        bool all = false,
        bool participating = false,
        DateTime? since = null,
        int perPage = 50,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(perPage, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(perPage, 50);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);

        var queryParams = new List<string>
        {
            $"per_page={perPage}",
            $"page={page}"
        };

        if (all)
        {
            queryParams.Add("all=true");
        }

        if (participating)
        {
            queryParams.Add("participating=true");
        }

        if (since.HasValue)
        {
            queryParams.Add($"since={since.Value:O}");
        }

        var url = $"notifications?{string.Join("&", queryParams)}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var rateLimitInfo = ParseRateLimitHeaders(response);
        LogRateLimitStatusForNotifications(rateLimitInfo);

        var notifications = await response.Content.ReadFromJsonAsync<List<GitHubNotification>>(cancellationToken);

        return notifications ?? [];
    }

    public async IAsyncEnumerable<GitHubNotification> GetAllNotificationsAsync(
        bool all = false,
        bool participating = false,
        DateTime? since = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        const int perPage = 50;
        var page = 1;

        while (!cancellationToken.IsCancellationRequested)
        {
            var notifications = await GetNotificationsAsync(all, participating, since, perPage, page, cancellationToken);

            if (notifications.Count == 0)
            {
                yield break;
            }

            foreach (var notification in notifications)
            {
                yield return notification;
            }

            if (notifications.Count < perPage)
            {
                yield break;
            }

            page++;
        }
    }

    public async Task<IReadOnlyList<GitHubNotification>> GetRepositoryNotificationsAsync(
        string owner,
        string repo,
        bool all = false,
        bool participating = false,
        DateTime? since = null,
        int perPage = 50,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentOutOfRangeException.ThrowIfLessThan(perPage, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(perPage, 50);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);

        var queryParams = new List<string>
        {
            $"per_page={perPage}",
            $"page={page}"
        };

        if (all)
        {
            queryParams.Add("all=true");
        }

        if (participating)
        {
            queryParams.Add("participating=true");
        }

        if (since.HasValue)
        {
            queryParams.Add($"since={since.Value:O}");
        }

        var url = $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/notifications?{string.Join("&", queryParams)}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var rateLimitInfo = ParseRateLimitHeaders(response);
        LogRateLimitStatus(rateLimitInfo, owner, repo);

        var notifications = await response.Content.ReadFromJsonAsync<List<GitHubNotification>>(cancellationToken);

        return notifications ?? [];
    }

    public async Task MarkNotificationAsReadAsync(
        string notificationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(notificationId);

        var url = $"notifications/threads/{Uri.EscapeDataString(notificationId)}";

        using var response = await _httpClient.PatchAsync(url, null, cancellationToken);
        response.EnsureSuccessStatusCode();

        var rateLimitInfo = ParseRateLimitHeaders(response);
        LogRateLimitStatusForNotifications(rateLimitInfo);
    }

    private void LogRateLimitStatusForNotifications(GitHubRateLimitInfo rateLimitInfo)
    {
        if (!rateLimitInfo.IsValid)
        {
            return;
        }

        if (rateLimitInfo.IsApproachingLimit(10))
        {
            _logger.LogWarning(
                "GitHub API rate limit approaching for notifications: {Remaining}/{Limit} requests remaining ({Percentage:F1}%). Resets at {ResetAt:u}",
                rateLimitInfo.Remaining,
                rateLimitInfo.Limit,
                rateLimitInfo.RemainingPercentage,
                rateLimitInfo.ResetAt);
        }
        else
        {
            _logger.LogDebug(
                "GitHub API rate limit status: {Remaining}/{Limit} requests remaining. Resets at {ResetAt:u}",
                rateLimitInfo.Remaining,
                rateLimitInfo.Limit,
                rateLimitInfo.ResetAt);
        }
    }
}
