namespace PatchNotes.Sync.GitHub;

/// <summary>
/// Options for configuring the GitHub client.
/// </summary>
public class GitHubClientOptions
{
    /// <summary>
    /// The section name in configuration.
    /// </summary>
    public const string SectionName = "GitHub";

    /// <summary>
    /// The GitHub API base URL. Defaults to https://api.github.com.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.github.com";

    /// <summary>
    /// Optional personal access token for authentication.
    /// Increases rate limits from 60 to 5000 requests per hour.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// The User-Agent header value. Required by GitHub API.
    /// </summary>
    public string UserAgent { get; set; } = "PatchNotes";
}
