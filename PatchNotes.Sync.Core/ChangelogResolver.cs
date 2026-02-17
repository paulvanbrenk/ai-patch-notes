using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PatchNotes.Sync.Core.GitHub;

namespace PatchNotes.Sync.Core;

/// <summary>
/// Detects release bodies that reference external changelogs and fetches the real content.
/// </summary>
public class ChangelogResolver
{
    private readonly IGitHubClient _github;
    private readonly ILogger<ChangelogResolver> _logger;

    private static readonly string[] ChangelogPaths =
    [
        "CHANGELOG.md",
        "CHANGES.md",
        "HISTORY.md",
        "changelog.md",
        "changes.md",
        "history.md",
        "Changelog.md"
    ];

    private static readonly Regex ChangelogReferencePattern = new(
        @"CHANGELOG\.md|HISTORY\.md|CHANGES\.md|Full changelog: https://github\.com/",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex MarkdownLinkPattern = new(
        @"\[(?<title>[^\]]+)\]\((?<url>[^)]+)\)",
        RegexOptions.Compiled);

    private static readonly Regex BareUrlPattern = new(
        @"https?://\S+",
        RegexOptions.Compiled);

    private static readonly string[] ChangelogLinkTitles =
        ["changelog", "changes", "history", "release notes", "release", "what's changed"];

    private static readonly string[] ChangelogUrlKeywords =
        ["changelog", "changes.md", "history.md", "release-notes"];

    private static readonly Regex GitHubBlobUrlPattern = new(
        @"https://github\.com/[^/]+/[^/]+/blob/[^/]+/(?<path>[^#?)]+)",
        RegexOptions.Compiled);

    private static readonly Regex GitHubReleaseUrlPattern = new(
        @"https://github\.com/(?<owner>[^/]+)/(?<repo>[^/]+)/releases/tag/(?<tag>[^)\s#]+)",
        RegexOptions.Compiled);

    // Matches headings like: ## [1.2.3], ## 1.2.3, # v1.2.3, ### 1.2.3 (2024-01-15)
    private static readonly Regex HeadingPattern = new(
        @"^(#{1,4})\s+\[?v?(?<version>[^\]\s(]+)\]?[^\r\n]*",
        RegexOptions.Multiline | RegexOptions.Compiled);

    public ChangelogResolver(IGitHubClient github, ILogger<ChangelogResolver> logger)
    {
        _github = github;
        _logger = logger;
    }

    /// <summary>
    /// Checks if a release body is essentially just a link to another GitHub release.
    /// </summary>
    public static (string owner, string repo, string tag)? ExtractGitHubReleaseLink(string? body)
    {
        if (string.IsNullOrWhiteSpace(body) || body.Length >= 300)
            return null;

        var match = GitHubReleaseUrlPattern.Match(body);
        if (!match.Success)
            return null;

        // Only treat as a release link if the body is basically just that link
        // (with optional markdown wrapper or minimal surrounding text)
        var stripped = body.Trim();
        var urlStart = match.Index;
        var urlEnd = match.Index + match.Length;
        var beforeUrl = stripped[..urlStart].TrimEnd('[', ' ', '\n', '\r');
        var afterUrl = stripped[urlEnd..].TrimStart(')', ' ', '\n', '\r');

        if (beforeUrl.Length > 30 || afterUrl.Length > 30)
            return null;

        return (match.Groups["owner"].Value, match.Groups["repo"].Value, match.Groups["tag"].Value);
    }

    /// <summary>
    /// Follows cross-repo GitHub release links up to maxHops times.
    /// Returns the final body if it has real content, otherwise the last body encountered.
    /// </summary>
    public async Task<string?> FollowReleaseLinksAsync(
        string? body,
        int maxHops = 5,
        CancellationToken cancellationToken = default)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var hop = 0; hop < maxHops; hop++)
        {
            var link = ExtractGitHubReleaseLink(body);
            if (link == null)
                break;

            var key = $"{link.Value.owner}/{link.Value.repo}/{link.Value.tag}";
            if (!visited.Add(key))
            {
                _logger.LogDebug("Circular release link detected at {Key}", key);
                break;
            }

            try
            {
                var release = await _github.GetReleaseByTagAsync(
                    link.Value.owner, link.Value.repo, link.Value.tag, cancellationToken);

                if (release == null || string.IsNullOrWhiteSpace(release.Body))
                {
                    _logger.LogDebug("Release {Key} not found or has empty body", key);
                    break;
                }

                _logger.LogDebug("Followed release link to {Key} (hop {Hop})", key, hop + 1);
                body = release.Body;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to follow release link to {Key}", key);
                break;
            }
        }

        return body;
    }

    /// <summary>
    /// Checks if a release body looks like a changelog reference rather than real content.
    /// </summary>
    public static bool IsChangelogReference(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;

        // Long bodies are real content, not references
        if (body.Length >= 300)
            return false;

        // Check markdown links for changelog-related titles or URLs
        foreach (Match match in MarkdownLinkPattern.Matches(body))
        {
            var title = match.Groups["title"].Value;
            var url = match.Groups["url"].Value;

            if (ChangelogLinkTitles.Any(t => title.Contains(t, StringComparison.OrdinalIgnoreCase)))
                return true;

            if (ChangelogUrlKeywords.Any(k => url.Contains(k, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        // Check bare URLs for changelog paths
        foreach (Match match in BareUrlPattern.Matches(body))
        {
            if (ChangelogUrlKeywords.Any(k => match.Value.Contains(k, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        // Catch non-URL patterns like "See CHANGELOG.md for details"
        return ChangelogReferencePattern.IsMatch(body);
    }

    /// <summary>
    /// Extracts the file path from a GitHub blob URL in the body, if present.
    /// e.g. https://github.com/vitejs/vite/blob/v7.3.1/packages/vite/CHANGELOG.md → packages/vite/CHANGELOG.md
    /// </summary>
    public static string? ExtractPathFromBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        var match = GitHubBlobUrlPattern.Match(body);
        return match.Success ? match.Groups["path"].Value : null;
    }

    /// <summary>
    /// Attempts to resolve a changelog reference by fetching the actual changelog content.
    /// Returns the extracted section or null if resolution fails.
    /// </summary>
    public async Task<string?> ResolveAsync(
        string owner,
        string repo,
        string tagName,
        string? body = null,
        CancellationToken cancellationToken = default)
    {
        // First, try to extract a file path from a GitHub URL in the body
        var urlPath = ExtractPathFromBody(body);
        if (urlPath != null)
        {
            try
            {
                var content = await _github.GetFileContentAsync(owner, repo, urlPath, cancellationToken);
                if (content != null)
                {
                    var section = ExtractVersionSection(content, tagName);
                    if (section != null)
                    {
                        _logger.LogDebug(
                            "Resolved changelog for {Owner}/{Repo} {Tag} from URL path {Path}",
                            owner, repo, tagName, urlPath);
                        return section;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to fetch changelog {Path} from {Owner}/{Repo}",
                    urlPath, owner, repo);
            }
        }

        // Fall back to standard changelog paths
        foreach (var path in ChangelogPaths)
        {
            try
            {
                var content = await _github.GetFileContentAsync(owner, repo, path, cancellationToken);
                if (content == null)
                    continue;

                var section = ExtractVersionSection(content, tagName);
                if (section != null)
                {
                    _logger.LogDebug(
                        "Resolved changelog for {Owner}/{Repo} {Tag} from {Path}",
                        owner, repo, tagName, path);
                    return section;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to fetch changelog {Path} from {Owner}/{Repo}",
                    path, owner, repo);
            }
        }

        _logger.LogDebug(
            "Could not resolve changelog for {Owner}/{Repo} {Tag}",
            owner, repo, tagName);
        return null;
    }

    /// <summary>
    /// Extracts the section for a specific version from changelog content.
    /// </summary>
    public static string? ExtractVersionSection(string content, string tagName)
    {
        // Normalize the version: strip leading 'v' from tag for matching
        var version = tagName.TrimStart('v');

        var matches = HeadingPattern.Matches(content);
        if (matches.Count == 0)
            return null;

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var headingVersion = match.Groups["version"].Value;

            if (!VersionMatches(headingVersion, version))
                continue;

            var headingLevel = match.Groups[1].Value.Length;
            var sectionStart = match.Index + match.Length;

            // Find the next heading at the same or higher level
            int sectionEnd = content.Length;
            for (int j = i + 1; j < matches.Count; j++)
            {
                var nextLevel = matches[j].Groups[1].Value.Length;
                if (nextLevel <= headingLevel)
                {
                    sectionEnd = matches[j].Index;
                    break;
                }
            }

            var section = content[sectionStart..sectionEnd].Trim();
            return string.IsNullOrEmpty(section) ? null : section;
        }

        return null;
    }

    private static bool VersionMatches(string headingVersion, string targetVersion)
    {
        // Exact match
        if (string.Equals(headingVersion, targetVersion, StringComparison.OrdinalIgnoreCase))
            return true;

        // Strip leading 'v' from heading version too
        var normalizedHeading = headingVersion.TrimStart('v');
        return string.Equals(normalizedHeading, targetVersion, StringComparison.OrdinalIgnoreCase);
    }
}
