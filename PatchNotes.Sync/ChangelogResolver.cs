using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PatchNotes.Data.GitHub;

namespace PatchNotes.Sync;

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
        "Changelog.md"
    ];

    private static readonly Regex ChangelogReferencePattern = new(
        @"CHANGELOG\.md|HISTORY\.md|CHANGES\.md|See .* for (full )?details|Full changelog: https://github\.com/",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
    /// Checks if a release body looks like a changelog reference rather than real content.
    /// </summary>
    public static bool IsChangelogReference(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return true;

        if (body.Length >= 100)
            return false;

        return ChangelogReferencePattern.IsMatch(body);
    }

    /// <summary>
    /// Attempts to resolve a changelog reference by fetching the actual changelog content.
    /// Returns the extracted section or null if resolution fails.
    /// </summary>
    public async Task<string?> ResolveAsync(
        string owner,
        string repo,
        string tagName,
        CancellationToken cancellationToken = default)
    {
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
