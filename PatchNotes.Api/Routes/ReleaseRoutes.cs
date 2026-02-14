using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PatchNotes.Data;
using PatchNotes.Data.Stytch;

namespace PatchNotes.Api.Routes;

public static class ReleaseRoutes
{
    public static WebApplication MapReleaseRoutes(this WebApplication app)
    {
        var requireAuth = RouteUtils.CreateAuthFilter();

        var group = app.MapGroup("/api/releases").WithTags("Releases");

        // GET /api/releases/{id} - Get single release details
        group.MapGet("/{id}", async (string id, PatchNotesDbContext db) =>
        {
            var release = await db.Releases
                .Include(r => r.Package)
                .Where(r => r.Id == id)
                .Select(r => new ReleaseDto
                {
                    Id = r.Id,
                    Tag = r.Tag,
                    Title = r.Title,
                    Body = r.Body,
                    PublishedAt = r.PublishedAt,
                    FetchedAt = r.FetchedAt,
                    Package = new ReleasePackageDto
                    {
                        Id = r.Package.Id,
                        NpmName = r.Package.NpmName,
                        GithubOwner = r.Package.GithubOwner,
                        GithubRepo = r.Package.GithubRepo
                    }
                })
                .FirstOrDefaultAsync();

            if (release == null)
            {
                return Results.NotFound(new { error = "Release not found" });
            }

            return Results.Ok(release);
        })
        .Produces<ReleaseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName("GetRelease");

        // GET /api/releases - Query releases for selected packages
        group.MapGet("/", async (string? packages, int? days, bool? excludePrerelease, int? majorVersion,
            bool? watchlist, HttpContext httpContext, PatchNotesDbContext db, IStytchClient stytchClient,
            IOptions<DefaultWatchlistOptions> watchlistOptions) =>
        {
            var daysToQuery = days ?? 7;
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-daysToQuery);

            IQueryable<Release> query = db.Releases
                .Include(r => r.Package)
                .Where(r => r.PublishedAt >= cutoffDate);

            if (watchlist == true && !string.IsNullOrEmpty(packages))
            {
                return Results.Json(new { error = "Cannot specify both 'watchlist' and 'packages' parameters" }, statusCode: 400);
            }

            if (watchlist == true)
            {
                // Explicit watchlist filter — require authentication
                var userWatchlistIds = await GetAuthenticatedUserWatchlistIds(httpContext, db, stytchClient);
                if (userWatchlistIds == null)
                {
                    return Results.Json(new { error = "Authentication required for watchlist filter" }, statusCode: 401);
                }

                query = query.Where(r => userWatchlistIds.Contains(r.PackageId));
            }
            else if (!string.IsNullOrEmpty(packages))
            {
                var packageIds = packages.Split(',')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList();

                if (packageIds.Count > 0)
                {
                    query = query.Where(r => packageIds.Contains(r.PackageId));
                }
            }
            else
            {
                // No explicit packages filter — use watchlist
                var (watchlistIds, hasWatchlistConfig) = await ResolveWatchlistPackageIds(
                    httpContext, db, stytchClient, watchlistOptions.Value);

                if (hasWatchlistConfig)
                {
                    // Watchlist is configured — filter to it (empty watchlist = empty results)
                    query = query.Where(r => watchlistIds.Contains(r.PackageId));
                }
                // else: no watchlist configured at all — show all releases
            }

            // Filter using denormalized version fields at DB level
            if (excludePrerelease == true)
            {
                query = query.Where(r => !r.IsPrerelease);
            }

            if (majorVersion.HasValue)
            {
                query = query.Where(r => r.MajorVersion == majorVersion.Value);
            }

            var releases = await query
                .OrderByDescending(r => r.PublishedAt)
                .Select(r => new ReleaseDto
                {
                    Id = r.Id,
                    Tag = r.Tag,
                    Title = r.Title,
                    Body = r.Body,
                    PublishedAt = r.PublishedAt,
                    FetchedAt = r.FetchedAt,
                    Package = new ReleasePackageDto
                    {
                        Id = r.Package.Id,
                        NpmName = r.Package.NpmName,
                        GithubOwner = r.Package.GithubOwner,
                        GithubRepo = r.Package.GithubRepo
                    }
                })
                .ToListAsync();

            return Results.Ok(releases);
        })
        .Produces<List<ReleaseDto>>(StatusCodes.Status200OK)
        .WithName("GetReleases");

        return app;
    }

    /// <summary>
    /// Attempts to authenticate the user and return their watchlist package IDs.
    /// Returns null if the user is not authenticated; returns an empty list if authenticated but no watchlist.
    /// </summary>
    private static async Task<List<string>?> GetAuthenticatedUserWatchlistIds(
        HttpContext httpContext, PatchNotesDbContext db, IStytchClient stytchClient)
    {
        var sessionToken = httpContext.Request.Cookies["stytch_session"];
        if (string.IsNullOrEmpty(sessionToken))
            return null;

        var session = await stytchClient.AuthenticateSessionAsync(sessionToken, httpContext.RequestAborted);
        if (session == null)
            return null;

        var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == session.UserId);
        if (user == null)
            return null;

        return await db.Watchlists
            .Where(w => w.UserId == user.Id)
            .Select(w => w.PackageId)
            .ToListAsync();
    }

    /// <summary>
    /// Resolves package IDs to filter by: user's watchlist if authenticated with a non-empty
    /// watchlist, otherwise the default watchlist from config.
    /// Returns (packageIds, hasWatchlistConfig) where hasWatchlistConfig indicates whether
    /// any watchlist source was available (user watchlist or default config).
    /// </summary>
    private static async Task<(List<string> ids, bool hasWatchlistConfig)> ResolveWatchlistPackageIds(
        HttpContext httpContext, PatchNotesDbContext db,
        IStytchClient stytchClient, DefaultWatchlistOptions defaultWatchlist)
    {
        var userWatchlistIds = await GetAuthenticatedUserWatchlistIds(httpContext, db, stytchClient);
        if (userWatchlistIds is { Count: > 0 })
        {
            return (userWatchlistIds, true);
        }

        // Fall back to default watchlist
        if (defaultWatchlist.Packages.Length == 0)
        {
            return ([], false);
        }

        var ownerRepoPairs = defaultWatchlist.Packages
            .Select(p => p.Split('/'))
            .Where(parts => parts.Length == 2)
            .Select(parts => parts[0] + "/" + parts[1])
            .ToList();

        var ids = await db.Packages
            .Where(p => ownerRepoPairs.Contains(p.GithubOwner + "/" + p.GithubRepo))
            .Select(p => p.Id)
            .ToListAsync();

        return (ids, true);
    }
}

public class ReleasePackageDto
{
    public required string Id { get; set; }
    public string? NpmName { get; set; }
    public required string GithubOwner { get; set; }
    public required string GithubRepo { get; set; }
}

public class ReleaseDto
{
    public required string Id { get; set; }
    public required string Tag { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
    public required ReleasePackageDto Package { get; set; }
}
