namespace PatchNotes.Sync.Core;

public static class GitHubUrlParser
{
    /// <summary>
    /// Parses a GitHub URL or owner/repo shorthand into owner and repo components.
    /// Supports formats: https://github.com/owner/repo, git+https://github.com/owner/repo.git,
    /// git://github.com/owner/repo.git, github:owner/repo, and owner/repo shorthand.
    /// </summary>
    /// <param name="url">GitHub URL or shorthand.</param>
    /// <returns>Tuple of (Owner, Repo).</returns>
    /// <exception cref="ArgumentException">Thrown when the URL cannot be parsed.</exception>
    public static (string Owner, string Repo) Parse(string url)
    {
        if (url is null)
        {
            throw new ArgumentException(
                "Invalid GitHub URL: 'null'. Expected format: https://github.com/owner/repo or owner/repo");
        }

        var normalized = NormalizeUrl(url.Trim());

        // Handle github:owner/repo shorthand
        if (normalized.StartsWith("github:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = normalized[7..].Split('/');
            if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
            {
                return (parts[0], TrimGitSuffix(parts[1]));
            }
        }

        // Support full URLs like https://github.com/prettier/prettier
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2)
            {
                return (segments[0], TrimGitSuffix(segments[1]));
            }
        }

        // Support owner/repo shorthand
        var shorthandParts = normalized.Trim('/').Split('/');
        if (shorthandParts.Length == 2 && shorthandParts[0].Length > 0 && shorthandParts[1].Length > 0)
        {
            return (shorthandParts[0], TrimGitSuffix(shorthandParts[1]));
        }

        throw new ArgumentException(
            $"Invalid GitHub URL: '{url}'. Expected format: https://github.com/owner/repo or owner/repo");
    }

    /// <summary>
    /// Tries to parse a GitHub URL or shorthand, returning null components on failure.
    /// </summary>
    public static (string? Owner, string? Repo) TryParse(string url)
    {
        try
        {
            var (owner, repo) = Parse(url);
            return (owner, repo);
        }
        catch (ArgumentException)
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Strips non-standard URL scheme prefixes (git+https://, git://) so that
    /// Uri.TryCreate can handle them.
    /// </summary>
    private static string NormalizeUrl(string url)
    {
        if (url.StartsWith("git+", StringComparison.OrdinalIgnoreCase))
            return url[4..];
        if (url.StartsWith("git://", StringComparison.OrdinalIgnoreCase))
            return "https://" + url[6..];
        return url;
    }

    private static string TrimGitSuffix(string s)
        => s.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? s[..^4] : s;
}
