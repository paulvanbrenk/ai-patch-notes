using System.Text.RegularExpressions;
using PatchNotes.Data;

namespace PatchNotes.Sync;

/// <summary>
/// A group of releases sharing the same package, major version, and pre-release status.
/// MajorVersion is -1 for releases with non-semver tags (grouped as "unversioned").
/// </summary>
public record VersionGroup(
    string PackageId,
    int MajorVersion,
    bool IsPrerelease,
    List<Release> Releases);

/// <summary>
/// Parses semantic versions from release tags and groups releases
/// by major version, separating pre-release from stable.
/// </summary>
public class VersionGroupingService
{
    private static readonly Regex SemverRegex = new(
        @"^v?(\d+)\.(\d+)(?:\.(\d+))?(?:-([a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?(?:\+[a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*)?$",
        RegexOptions.Compiled);

    private static readonly Regex MonorepoRegex = new(
        @"^@?[\w-]+(?:/[\w-]+)*[@/]v?(\d+)\.(\d+)(?:\.(\d+))?(?:-([a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?(?:\+[a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*)?$",
        RegexOptions.Compiled);

    private static readonly Regex ReleaseTagRegex = new(
        @"^release[-/]v?(\d+)\.(\d+)(?:\.(\d+))?(?:-([a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Non-standard prerelease patterns like "1.0.0beta1" or "1.0.0.rc1"
    private static readonly Regex NonStandardPrereleaseRegex = new(
        @"^v?(\d+)\.(\d+)\.(\d+)[.\s]?(alpha|beta|canary|rc|next|nightly|dev|preview)\d*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Groups releases by (PackageId, MajorVersion, IsPrerelease).
    /// Deduplicates releases sharing the same (PackageId, Tag).
    /// Non-semver tags are grouped with MajorVersion = -1.
    /// </summary>
    public IEnumerable<VersionGroup> GroupReleases(IEnumerable<Release> releases)
    {
        // Deduplicate by (PackageId, Tag)
        var deduped = releases
            .GroupBy(r => (r.PackageId, r.Tag))
            .Select(g => g.First());

        var groups = new Dictionary<(string PackageId, int MajorVersion, bool IsPrerelease), List<Release>>();

        foreach (var release in deduped)
        {
            // Use stored version fields from denormalized columns
            var key = (release.PackageId, release.MajorVersion, release.IsPrerelease);

            if (!groups.TryGetValue(key, out var list))
            {
                list = [];
                groups[key] = list;
            }

            list.Add(release);
        }

        return groups.Select(kvp => new VersionGroup(
            kvp.Key.PackageId,
            kvp.Key.MajorVersion,
            kvp.Key.IsPrerelease,
            kvp.Value));
    }

    public record ParsedTag(int MajorVersion, int MinorVersion, int PatchVersion, bool IsPrerelease);

    public ParsedTag ParseTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return new ParsedTag(-1, 0, 0, false);

        tag = tag.Trim();

        // Try non-standard prerelease first (e.g., "1.0.0beta1", "1.0.0.rc1")
        var nonStd = NonStandardPrereleaseRegex.Match(tag);
        if (nonStd.Success
            && int.TryParse(nonStd.Groups[1].Value, out var nsMajor)
            && int.TryParse(nonStd.Groups[2].Value, out var nsMinor)
            && int.TryParse(nonStd.Groups[3].Value, out var nsPatch))
            return new ParsedTag(nsMajor, nsMinor, nsPatch, true);

        // Try monorepo format
        var mono = MonorepoRegex.Match(tag);
        if (mono.Success && int.TryParse(mono.Groups[1].Value, out var monoMajor))
        {
            int.TryParse(mono.Groups[2].Value, out var monoMinor);
            int.TryParse(mono.Groups[3].Value, out var monoPatch);
            return new ParsedTag(monoMajor, monoMinor, monoPatch, mono.Groups[4].Success);
        }

        // Try release-style tags
        var rel = ReleaseTagRegex.Match(tag);
        if (rel.Success && int.TryParse(rel.Groups[1].Value, out var relMajor))
        {
            int.TryParse(rel.Groups[2].Value, out var relMinor);
            int.TryParse(rel.Groups[3].Value, out var relPatch);
            return new ParsedTag(relMajor, relMinor, relPatch, rel.Groups[4].Success);
        }

        // Try standard semver (including MAJOR.MINOR only)
        var sv = SemverRegex.Match(tag);
        if (sv.Success && int.TryParse(sv.Groups[1].Value, out var svMajor))
        {
            int.TryParse(sv.Groups[2].Value, out var svMinor);
            var svPatch = 0;
            if (sv.Groups[3].Success)
                int.TryParse(sv.Groups[3].Value, out svPatch);
            return new ParsedTag(svMajor, svMinor, svPatch, sv.Groups[4].Success);
        }

        // Non-semver → unversioned
        return new ParsedTag(-1, 0, 0, false);
    }
}
