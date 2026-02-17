namespace PatchNotes.Sync.Core;

public static class GitHubUrlParser
{
    /// <summary>
    /// Parses a GitHub URL or owner/repo shorthand into owner and repo components.
    /// </summary>
    /// <param name="url">GitHub URL (e.g. "https://github.com/owner/repo") or shorthand (e.g. "owner/repo").</param>
    /// <returns>Tuple of (Owner, Repo).</returns>
    /// <exception cref="ArgumentException">Thrown when the URL cannot be parsed.</exception>
    public static (string Owner, string Repo) Parse(string url)
    {
        if (url is null)
        {
            throw new ArgumentException(
                "Invalid GitHub URL: 'null'. Expected format: https://github.com/owner/repo or owner/repo");
        }

        // Support full URLs like https://github.com/prettier/prettier
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2)
            {
                return (segments[0], TrimGitSuffix(segments[1]));
            }
        }

        // Support owner/repo shorthand
        var parts = url.Trim('/').Split('/');
        if (parts.Length == 2 && parts[0].Length > 0 && parts[1].Length > 0)
        {
            return (parts[0], TrimGitSuffix(parts[1]));
        }

        throw new ArgumentException(
            $"Invalid GitHub URL: '{url}'. Expected format: https://github.com/owner/repo or owner/repo");
    }

    private static string TrimGitSuffix(string s)
        => s.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? s[..^4] : s;
}
