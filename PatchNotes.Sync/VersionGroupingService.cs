using System.Text.RegularExpressions;
using PatchNotes.Data;

namespace PatchNotes.Sync;

/// <summary>
/// Represents a group of releases sharing the same package, major version, and pre-release status.
/// </summary>
public record VersionGroup(
    string PackageId,
    int MajorVersion,
    bool IsPrerelease,
    List<Release> Releases);

/// <summary>
/// Groups releases by major version, separating pre-release from stable.
/// Parses semantic versions from release tags with support for standard semver,
/// monorepo tags, simple versions, and release-style tags.
/// </summary>
public class VersionGroupingService
{
    private static readonly string[] PrereleaseKeywords =
    [
        "alpha", "beta", "canary", "preview", "rc", "next",
        "nightly", "dev", "experimental", "snapshot", "pre", "insiders"
    ];

    // Standard semver: v?MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]
    private static readonly Regex SemverRegex = new(
        @"^v?(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?(?:\+([a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?$",
        RegexOptions.Compiled);

    // Monorepo tags: @scope/package@v?MAJOR.MINOR.PATCH
    private static readonly Regex MonorepoRegex = new(
        @"^(@?[\w-]+(?:/[\w-]+)*)[@/]v?(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?(?:\+([a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?$",
        RegexOptions.Compiled);

    // Simple semver: v?MAJOR.MINOR[-PRERELEASE]
    private static readonly Regex SimpleSemverRegex = new(
        @"^v?(\d+)\.(\d+)(?:-([a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?$",
        RegexOptions.Compiled);

    // Release-style tags: release-v?MAJOR.MINOR.PATCH
    private static readonly Regex ReleaseTagRegex = new(
        @"^release[-/]v?(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Non-standard pre-release patterns: 1.0.0beta1, 1.0.0.rc1
    private static readonly Regex NonStandardPrereleaseRegex = new(
        @"^v?(\d+)\.(\d+)\.(\d+)[\.]?(alpha|beta|canary|preview|rc|next|nightly|dev|pre)\.?(\d*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Groups releases by (PackageId, MajorVersion, IsPrerelease).
    /// Non-semver tags are grouped with MajorVersion = -1 as "unversioned".
    /// Duplicate tags within a package are deduplicated.
    /// </summary>
    public IEnumerable<VersionGroup> GroupReleases(IEnumerable<Release> releases)
    {
        var seen = new HashSet<(string PackageId, string Tag)>();
        var groups = new Dictionary<(string PackageId, int MajorVersion, bool IsPrerelease), List<Release>>();

        foreach (var release in releases)
        {
            // Deduplicate by (PackageId, Tag)
            var key = (release.PackageId, release.Tag);
            if (!seen.Add(key))
                continue;

            var parsed = ParseTag(release.Tag);

            var groupKey = (release.PackageId, parsed.MajorVersion, parsed.IsPrerelease);
            if (!groups.TryGetValue(groupKey, out var list))
            {
                list = [];
                groups[groupKey] = list;
            }

            list.Add(release);
        }

        return groups.Select(kvp => new VersionGroup(
            kvp.Key.PackageId,
            kvp.Key.MajorVersion,
            kvp.Key.IsPrerelease,
            kvp.Value));
    }

    internal record ParsedTag(int MajorVersion, bool IsPrerelease);

    internal ParsedTag ParseTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return new ParsedTag(-1, false);

        tag = tag.Trim();

        // Try non-standard pre-release first (1.0.0beta1, 1.0.0.rc1)
        var nonStdMatch = NonStandardPrereleaseRegex.Match(tag);
        if (nonStdMatch.Success && int.TryParse(nonStdMatch.Groups[1].Value, out var nsMajor))
            return new ParsedTag(nsMajor, true);

        // Try monorepo format
        var monorepoMatch = MonorepoRegex.Match(tag);
        if (monorepoMatch.Success && int.TryParse(monorepoMatch.Groups[2].Value, out var monoMajor))
        {
            var prerelease = monorepoMatch.Groups[5].Success ? monorepoMatch.Groups[5].Value : null;
            return new ParsedTag(monoMajor, !string.IsNullOrEmpty(prerelease));
        }

        // Try release-style tags
        var releaseMatch = ReleaseTagRegex.Match(tag);
        if (releaseMatch.Success && int.TryParse(releaseMatch.Groups[1].Value, out var relMajor))
        {
            var prerelease = releaseMatch.Groups[4].Success ? releaseMatch.Groups[4].Value : null;
            return new ParsedTag(relMajor, !string.IsNullOrEmpty(prerelease));
        }

        // Try standard semver
        var semverMatch = SemverRegex.Match(tag);
        if (semverMatch.Success && int.TryParse(semverMatch.Groups[1].Value, out var svMajor))
        {
            var prerelease = semverMatch.Groups[4].Success ? semverMatch.Groups[4].Value : null;
            var isPrerelease = !string.IsNullOrEmpty(prerelease);

            // Also check keyword heuristic for edge cases
            if (!isPrerelease)
            {
                var lowerTag = tag.ToLowerInvariant();
                isPrerelease = PrereleaseKeywords.Any(kw => lowerTag.Contains(kw));
            }

            return new ParsedTag(svMajor, isPrerelease);
        }

        // Try simple semver (MAJOR.MINOR only)
        var simpleMatch = SimpleSemverRegex.Match(tag);
        if (simpleMatch.Success && int.TryParse(simpleMatch.Groups[1].Value, out var simpleMajor))
        {
            var prerelease = simpleMatch.Groups[3].Success ? simpleMatch.Groups[3].Value : null;
            return new ParsedTag(simpleMajor, !string.IsNullOrEmpty(prerelease));
        }

        // Non-semver tag → unversioned (MajorVersion = -1)
        return new ParsedTag(-1, false);
    }
}
