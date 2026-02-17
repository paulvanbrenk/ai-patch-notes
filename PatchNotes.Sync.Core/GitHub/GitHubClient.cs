using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using PatchNotes.Sync.Core.GitHub.Models;

namespace PatchNotes.Sync.Core.GitHub;

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

        var rateLimitInfo = RateLimitHelper.ParseHeaders(response.Headers);
        RateLimitHelper.LogStatus(_logger, rateLimitInfo, $"{owner}/{repo}");

        var releases = await response.Content.ReadFromJsonAsync<List<GitHubRelease>>(cancellationToken);

        return releases ?? [];
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

    public async Task<GitHubRelease?> GetReleaseByTagAsync(
        string owner,
        string repo,
        string tag,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        var url = $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/releases/tags/{Uri.EscapeDataString(tag)}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        var rateLimitInfo = RateLimitHelper.ParseHeaders(response.Headers);
        RateLimitHelper.LogStatus(_logger, rateLimitInfo, $"{owner}/{repo}");

        return await response.Content.ReadFromJsonAsync<GitHubRelease>(cancellationToken);
    }

    public async Task<IReadOnlyList<GitHubSearchResult>> SearchRepositoriesAsync(
        string query,
        int perPage = 10,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentOutOfRangeException.ThrowIfLessThan(perPage, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(perPage, 100);

        var url = $"search/repositories?q={Uri.EscapeDataString(query)}&per_page={perPage}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var rateLimitInfo = RateLimitHelper.ParseHeaders(response.Headers);
        RateLimitHelper.LogStatus(_logger, rateLimitInfo, "search");

        var searchResponse = await response.Content.ReadFromJsonAsync<GitHubSearchResponse>(cancellationToken);

        return searchResponse?.Items ?? [];
    }

    public async Task<string?> GetFileContentAsync(
        string owner,
        string repo,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(repo);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var url = $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}/contents/{path}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.raw"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        // Skip files that are too large (>1MB)
        if (response.Content.Headers.ContentLength > 1_048_576)
        {
            _logger.LogWarning("File too large to fetch: {Owner}/{Repo}/{Path} ({Size} bytes)",
                owner, repo, path, response.Content.Headers.ContentLength);
            return null;
        }

        response.EnsureSuccessStatusCode();

        var rateLimitInfo = RateLimitHelper.ParseHeaders(response.Headers);
        RateLimitHelper.LogStatus(_logger, rateLimitInfo, $"{owner}/{repo}");

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
