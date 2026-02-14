using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PatchNotes.Data;
using PatchNotes.Api.Stytch;
using static PatchNotes.Data.SummaryConstants;

namespace PatchNotes.Api.Routes;

public static class FeedRoutes
{
    public static WebApplication MapFeedRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/feed").WithTags("Feed");

        // GET /api/feed - Combined feed with server-side grouping and summaries
        group.MapGet("/", async (
            bool? excludePrerelease,
            HttpContext httpContext,
            PatchNotesDbContext db,
            IStytchClient stytchClient,
            IOptions<DefaultWatchlistOptions> watchlistOptions) =>
        {
            // Resolve which packages to show
            var (watchlistIds, hasWatchlistConfig) = await RouteUtils.ResolveWatchlistPackageIds(
                httpContext, db, stytchClient, watchlistOptions.Value);

            IQueryable<Release> releaseQuery = db.Releases
                .Include(r => r.Package);

            if (hasWatchlistConfig)
            {
                releaseQuery = releaseQuery.Where(r => watchlistIds.Contains(r.PackageId));
            }

            if (excludePrerelease == true)
            {
                releaseQuery = releaseQuery.Where(r => !r.IsPrerelease);
            }

            // Group releases server-side by (packageId, majorVersion, isPrerelease)
            var groups = await releaseQuery
                .GroupBy(r => new { r.PackageId, r.MajorVersion, r.IsPrerelease })
                .Select(g => new
                {
                    g.Key.PackageId,
                    g.Key.MajorVersion,
                    g.Key.IsPrerelease,
                    ReleaseCount = g.Count(),
                    LastUpdated = g.Max(r => r.PublishedAt),
                    Releases = g.OrderByDescending(r => r.PublishedAt)
                        .Select(r => new FeedReleaseDto
                        {
                            Id = r.Id,
                            Tag = r.Tag,
                            Title = r.Title,
                            PublishedAt = r.PublishedAt,
                        })
                        .ToList(),
                    // Grab package info from any member of the group
                    PackageName = g.First().Package.Name,
                    NpmName = g.First().Package.NpmName,
                    GithubOwner = g.First().Package.GithubOwner,
                    GithubRepo = g.First().Package.GithubRepo,
                })
                .OrderByDescending(g => g.LastUpdated)
                .ToListAsync();

            // Left-join ReleaseSummary to attach AI summaries per group
            var groupKeys = groups.Select(g => new { g.PackageId, g.MajorVersion, g.IsPrerelease }).ToList();

            var summaries = await db.ReleaseSummaries
                .Where(s => groupKeys.Select(k => k.PackageId).Contains(s.PackageId))
                .ToListAsync();

            var summaryLookup = summaries.ToDictionary(
                s => (s.PackageId, s.MajorVersion, s.IsPrerelease),
                s => s.Summary);

            var feedGroups = groups.Select(g =>
            {
                summaryLookup.TryGetValue((g.PackageId, g.MajorVersion, g.IsPrerelease), out var summary);

                // Limit displayed releases to the same window used for summary generation
                var cutoff = g.LastUpdated - SummaryWindow;
                var windowedReleases = g.Releases
                    .Where(r => r.PublishedAt >= cutoff)
                    .ToList();
                if (windowedReleases.Count == 0)
                    windowedReleases = g.Releases.Take(1).ToList();

                return new FeedGroupDto
                {
                    PackageId = g.PackageId,
                    PackageName = g.PackageName,
                    NpmName = g.NpmName,
                    GithubOwner = g.GithubOwner,
                    GithubRepo = g.GithubRepo,
                    MajorVersion = g.MajorVersion,
                    IsPrerelease = g.IsPrerelease,
                    VersionRange = $"v{g.MajorVersion}.x",
                    Summary = summary,
                    ReleaseCount = g.ReleaseCount,
                    LastUpdated = g.LastUpdated,
                    Releases = windowedReleases,
                };
            }).ToList();

            return Results.Ok(new FeedResponseDto { Groups = feedGroups });
        })
        .Produces<FeedResponseDto>(StatusCodes.Status200OK)
        .WithName("GetFeed");

        return app;
    }
}

public class FeedResponseDto
{
    public required List<FeedGroupDto> Groups { get; set; }
}

public class FeedGroupDto
{
    public required string PackageId { get; set; }
    public required string PackageName { get; set; }
    public string? NpmName { get; set; }
    public required string GithubOwner { get; set; }
    public required string GithubRepo { get; set; }
    public int MajorVersion { get; set; }
    public bool IsPrerelease { get; set; }
    public required string VersionRange { get; set; }
    public string? Summary { get; set; }
    public int ReleaseCount { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public required List<FeedReleaseDto> Releases { get; set; }
}

public class FeedReleaseDto
{
    public required string Id { get; set; }
    public required string Tag { get; set; }
    public string? Title { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
}
