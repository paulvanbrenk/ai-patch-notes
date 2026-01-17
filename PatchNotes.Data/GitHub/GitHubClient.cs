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
}
