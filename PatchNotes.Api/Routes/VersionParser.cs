using System.Text.RegularExpressions;

namespace PatchNotes.Api.Routes;

/// <summary>
/// Represents a parsed semantic version with all components.
/// </summary>
public record ParsedVersion(
    int Major,
    int Minor,
    int Patch,
    string? Prerelease,
    string? BuildMetadata,
    string OriginalTag,
    string? MonorepoPackage = null)
{
    /// <summary>
    /// Returns true if this is a pre-release version (has pre-release identifier).
    /// </summary>
    public bool IsPrerelease => !string.IsNullOrEmpty(Prerelease);

    /// <summary>
    /// Returns the version group key (e.g., "15.x" for stable, "15.x-canary" for pre-release).
    /// </summary>
    public string GetVersionGroupKey()
    {
        var baseGroup = $"{Major}.x";
        if (IsPrerelease)
        {
            // Extract the pre-release type (alpha, beta, rc, canary, etc.)
            var prereleaseType = GetPrereleaseType();
            return $"{baseGroup}-{prereleaseType}";
        }
        return baseGroup;
    }

    /// <summary>
    /// Extracts the pre-release type from the prerelease string.
    /// E.g., "alpha.1" -> "alpha", "rc.2" -> "rc", "canary.3" -> "canary"
    /// </summary>
    public string GetPrereleaseType()
    {
        if (string.IsNullOrEmpty(Prerelease))
            return "stable";

        // Match the first identifier (word) in the prerelease
        var match = Regex.Match(Prerelease, @"^([a-zA-Z]+)");
        return match.Success ? match.Groups[1].Value.ToLowerInvariant() : "prerelease";
    }
}

/// <summary>
/// Result of parsing a version tag.
/// </summary>
public record VersionParseResult
{
    public bool Success { get; init; }
    public ParsedVersion? Version { get; init; }
    public string? Error { get; init; }
    public string OriginalTag { get; init; } = string.Empty;

    public static VersionParseResult Ok(ParsedVersion version) =>
        new() { Success = true, Version = version, OriginalTag = version.OriginalTag };

    public static VersionParseResult Fail(string tag, string error) =>
        new() { Success = false, Error = error, OriginalTag = tag };
}

/// <summary>
/// Represents a group of releases by version.
/// </summary>
public record VersionGroup(
    string GroupKey,
    int MajorVersion,
    bool IsPrerelease,
    string? PrereleaseType,
    List<ParsedVersion> Versions);

/// <summary>
/// Parses semantic versions from release tags with support for various formats.
/// </summary>
public static class VersionParser
{
    // Pre-release keywords for heuristic detection
    private static readonly string[] PrereleaseKeywords =
    [
        "alpha", "beta", "canary", "preview", "rc", "next",
        "nightly", "dev", "experimental", "snapshot", "pre", "insiders"
    ];

    // Regex for standard semver: v?MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]
    private static readonly Regex SemverRegex = new(
        @"^v?(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?(?:\+([a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?$",
        RegexOptions.Compiled);

    // Regex for monorepo tags: @scope/package@v?MAJOR.MINOR.PATCH or @package/v?MAJOR.MINOR.PATCH
    private static readonly Regex MonorepoRegex = new(
        @"^(@?[\w-]+(?:/[\w-]+)*)[@/]v?(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?(?:\+([a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?$",
        RegexOptions.Compiled);

    // Regex for simpler version formats: v?MAJOR.MINOR[-PRERELEASE]
    private static readonly Regex SimpleSemverRegex = new(
        @"^v?(\d+)\.(\d+)(?:-([a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?$",
        RegexOptions.Compiled);

    // Regex for release-style tags: release-v?MAJOR.MINOR.PATCH
    private static readonly Regex ReleaseTagRegex = new(
        @"^release[-/]v?(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Parses a release tag into a semantic version.
    /// </summary>
    public static VersionParseResult Parse(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return VersionParseResult.Fail(tag, "Tag is empty or whitespace");

        tag = tag.Trim();

        // Try monorepo format first (most specific)
        var monorepoMatch = MonorepoRegex.Match(tag);
        if (monorepoMatch.Success)
        {
            return ParseMonorepoMatch(monorepoMatch, tag);
        }

        // Try release-style tags
        var releaseMatch = ReleaseTagRegex.Match(tag);
        if (releaseMatch.Success)
        {
            return ParseReleaseMatch(releaseMatch, tag);
        }

        // Try standard semver
        var semverMatch = SemverRegex.Match(tag);
        if (semverMatch.Success)
        {
            return ParseSemverMatch(semverMatch, tag);
        }

        // Try simple semver (MAJOR.MINOR only)
        var simpleMatch = SimpleSemverRegex.Match(tag);
        if (simpleMatch.Success)
        {
            return ParseSimpleSemverMatch(simpleMatch, tag);
        }

        return VersionParseResult.Fail(tag, "Tag does not match any known version format");
    }

    private static VersionParseResult ParseMonorepoMatch(Match match, string tag)
    {
        var package = match.Groups[1].Value;
        if (!int.TryParse(match.Groups[2].Value, out var major) ||
            !int.TryParse(match.Groups[3].Value, out var minor) ||
            !int.TryParse(match.Groups[4].Value, out var patch))
        {
            return VersionParseResult.Fail(tag, "Failed to parse version numbers");
        }

        var prerelease = match.Groups[5].Success ? match.Groups[5].Value : null;
        var build = match.Groups[6].Success ? match.Groups[6].Value : null;

        return VersionParseResult.Ok(new ParsedVersion(
            major, minor, patch, prerelease, build, tag, package));
    }

    private static VersionParseResult ParseReleaseMatch(Match match, string tag)
    {
        if (!int.TryParse(match.Groups[1].Value, out var major) ||
            !int.TryParse(match.Groups[2].Value, out var minor) ||
            !int.TryParse(match.Groups[3].Value, out var patch))
        {
            return VersionParseResult.Fail(tag, "Failed to parse version numbers");
        }

        var prerelease = match.Groups[4].Success ? match.Groups[4].Value : null;

        return VersionParseResult.Ok(new ParsedVersion(
            major, minor, patch, prerelease, null, tag));
    }

    private static VersionParseResult ParseSemverMatch(Match match, string tag)
    {
        if (!int.TryParse(match.Groups[1].Value, out var major) ||
            !int.TryParse(match.Groups[2].Value, out var minor) ||
            !int.TryParse(match.Groups[3].Value, out var patch))
        {
            return VersionParseResult.Fail(tag, "Failed to parse version numbers");
        }

        var prerelease = match.Groups[4].Success ? match.Groups[4].Value : null;
        var build = match.Groups[5].Success ? match.Groups[5].Value : null;

        return VersionParseResult.Ok(new ParsedVersion(
            major, minor, patch, prerelease, build, tag));
    }

    private static VersionParseResult ParseSimpleSemverMatch(Match match, string tag)
    {
        if (!int.TryParse(match.Groups[1].Value, out var major) ||
            !int.TryParse(match.Groups[2].Value, out var minor))
        {
            return VersionParseResult.Fail(tag, "Failed to parse version numbers");
        }

        var prerelease = match.Groups[3].Success ? match.Groups[3].Value : null;

        return VersionParseResult.Ok(new ParsedVersion(
            major, minor, 0, prerelease, null, tag));
    }

    /// <summary>
    /// Extracts the major version from a tag. Returns null if parsing fails.
    /// This is an improved version of RouteUtils.GetMajorVersion that handles more formats.
    /// </summary>
    public static int? GetMajorVersion(string tag)
    {
        var result = Parse(tag);
        return result.Success ? result.Version!.Major : null;
    }

    /// <summary>
    /// Determines if a tag represents a pre-release version.
    /// Uses both semantic versioning rules and heuristic keyword detection.
    /// </summary>
    public static bool IsPrerelease(string tag)
    {
        var result = Parse(tag);
        if (result.Success && result.Version!.IsPrerelease)
            return true;

        // Fallback to keyword-based detection for edge cases
        var lowerTag = tag.ToLowerInvariant();
        return PrereleaseKeywords.Any(keyword => lowerTag.Contains(keyword));
    }

    /// <summary>
    /// Groups a collection of release tags by major version.
    /// Separates stable releases from pre-releases.
    /// </summary>
    public static Dictionary<string, VersionGroup> GroupByMajorVersion(IEnumerable<string> tags)
    {
        var groups = new Dictionary<string, VersionGroup>();

        foreach (var tag in tags)
        {
            var result = Parse(tag);
            if (!result.Success)
                continue;

            var version = result.Version!;
            var groupKey = version.GetVersionGroupKey();

            if (!groups.TryGetValue(groupKey, out var group))
            {
                group = new VersionGroup(
                    groupKey,
                    version.Major,
                    version.IsPrerelease,
                    version.IsPrerelease ? version.GetPrereleaseType() : null,
                    []);
                groups[groupKey] = group;
            }

            group.Versions.Add(version);
        }

        return groups;
    }

    /// <summary>
    /// Groups releases and returns them sorted by major version (descending)
    /// with stable releases before their corresponding pre-releases.
    /// </summary>
    public static List<VersionGroup> GroupAndSort(IEnumerable<string> tags)
    {
        var groups = GroupByMajorVersion(tags);

        return groups.Values
            .OrderByDescending(g => g.MajorVersion)
            .ThenBy(g => g.IsPrerelease) // stable before pre-release
            .ThenBy(g => g.PrereleaseType)
            .ToList();
    }
}
