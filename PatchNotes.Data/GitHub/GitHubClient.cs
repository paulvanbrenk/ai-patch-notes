using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace PatchNotes.Data.GitHub;

/// <summary>
/// Client for interacting with the GitHub API.
/// </summary>
public class GitHubClient : IGitHubClient
{
    private readonly HttpClient _httpClient;

    public GitHubClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
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

        var releases = await _httpClient.GetFromJsonAsync<List<GitHubRelease>>(url, cancellationToken);

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
}
