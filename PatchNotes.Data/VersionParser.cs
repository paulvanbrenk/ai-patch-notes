using System.Text.RegularExpressions;

namespace PatchNotes.Data;

public static class VersionParser
{
    private static readonly Regex SemverRegex = new(
        @"^v?(\d+)\.(\d+)(?:\.(\d+))?(?:-([a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?",
        RegexOptions.Compiled);

    public static (int Major, int Minor, bool IsPrerelease) ParseVersion(string tag)
    {
        var match = SemverRegex.Match(tag);
        if (!match.Success)
            return (0, 0, false);

        int.TryParse(match.Groups[1].Value, out var major);
        int.TryParse(match.Groups[2].Value, out var minor);
        var isPrerelease = match.Groups[4].Success;

        return (major, minor, isPrerelease);
    }
}
