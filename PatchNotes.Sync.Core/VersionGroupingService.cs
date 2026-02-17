using PatchNotes.Data;

namespace PatchNotes.Sync.Core;

/// <summary>
/// A group of releases sharing the same package, major version, and pre-release status.
/// MajorVersion is -1 for releases with non-semver tags (grouped as "unversioned").
/// </summary>
public record ReleaseVersionGroup(
    string PackageId,
    int MajorVersion,
    bool IsPrerelease,
    List<Release> Releases);

/// <summary>
/// Groups releases by major version, separating pre-release from stable.
/// </summary>
public class VersionGroupingService
{
    /// <summary>
    /// Groups releases by (PackageId, MajorVersion, IsPrerelease).
    /// Deduplicates releases sharing the same (PackageId, Tag).
    /// Non-semver tags are grouped with MajorVersion = -1.
    /// </summary>
    public IEnumerable<ReleaseVersionGroup> GroupReleases(IEnumerable<Release> releases)
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

        return groups.Select(kvp => new ReleaseVersionGroup(
            kvp.Key.PackageId,
            kvp.Key.MajorVersion,
            kvp.Key.IsPrerelease,
            kvp.Value));
    }
}
