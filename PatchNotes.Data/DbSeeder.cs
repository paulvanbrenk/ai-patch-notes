using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PatchNotes.Data.SeedData;

namespace PatchNotes.Data;

public static class DbSeeder
{
    private static readonly Regex SemverRegex = new(
        @"^v?(\d+)\.(\d+)(?:\.(\d+))?(?:-([a-zA-Z0-9]+(?:\.[a-zA-Z0-9]+)*))?",
        RegexOptions.Compiled);

    public static async Task SeedAsync(PatchNotesDbContext context)
    {
        if (await context.Packages.AnyAsync())
        {
            return; // Already seeded
        }

        var now = DateTime.UtcNow;
        var packages = await LoadSeedDataAsync(now);

        context.Packages.AddRange(packages);
        await context.SaveChangesAsync();
    }

    private static async Task<List<Package>> LoadSeedDataAsync(DateTime now)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "PatchNotes.Data.SeedData.packages.json";

        await using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        }

        var seedPackages = await JsonSerializer.DeserializeAsync<List<SeedPackage>>(stream);
        if (seedPackages == null)
        {
            return [];
        }

        return seedPackages.Select(sp => new Package
        {
            Name = sp.Name,
            Url = sp.Url,
            NpmName = sp.NpmName,
            GithubOwner = sp.GithubOwner,
            GithubRepo = sp.GithubRepo,
            CreatedAt = now,
            LastFetchedAt = now,
            Releases = sp.Releases.Select(sr =>
            {
                var (major, minor, isPrerelease) = ParseVersion(sr.Tag);
                return new Release
                {
                    Version = sr.Tag,
                    Title = sr.Title,
                    Body = sr.Body,
                    PublishedAt = sr.PublishedAt,
                    FetchedAt = now,
                    Major = major,
                    Minor = minor,
                    IsPrerelease = isPrerelease
                };
            }).ToList()
        }).ToList();
    }

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
