using System.Text.Json.Serialization;

namespace PatchNotes.Sync.GitHub.Models;

/// <summary>
/// Represents the response from GitHub's repository search API.
/// </summary>
public class GitHubSearchResponse
{
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("items")]
    public List<GitHubSearchResult> Items { get; set; } = [];
}

/// <summary>
/// Represents a single repository result from GitHub's search API.
/// </summary>
public class GitHubSearchResult
{
    [JsonPropertyName("full_name")]
    public required string FullName { get; set; }

    [JsonPropertyName("owner")]
    public required GitHubSearchOwner Owner { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("stargazers_count")]
    public int StargazersCount { get; set; }
}

/// <summary>
/// Represents the owner of a repository in search results.
/// </summary>
public class GitHubSearchOwner
{
    [JsonPropertyName("login")]
    public required string Login { get; set; }
}
