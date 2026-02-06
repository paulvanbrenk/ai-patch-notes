using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PatchNotes.Data.SeedData;

namespace PatchNotes.Data;

public static class DbSeeder
{
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
            Releases = sp.Releases.Select(sr => new Release
            {
                Tag = sr.Tag,
                Title = sr.Title,
                Body = sr.Body,
                PublishedAt = sr.PublishedAt,
                FetchedAt = now
            }).ToList()
        }).ToList();
    }
}
