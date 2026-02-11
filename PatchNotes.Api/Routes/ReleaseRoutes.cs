using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PatchNotes.Data;
using PatchNotes.Data.AI;
using PatchNotes.Data.Stytch;

namespace PatchNotes.Api.Routes;

public static class ReleaseRoutes
{
    public static WebApplication MapReleaseRoutes(this WebApplication app)
    {
        var requireAuth = RouteUtils.CreateAuthFilter();

        // GET /api/releases/{id} - Get single release details
        app.MapGet("/api/releases/{id}", async (string id, PatchNotesDbContext db) =>
        {
            var release = await db.Releases
                .Include(r => r.Package)
                .Where(r => r.Id == id)
                .Select(r => new
                {
                    r.Id,
                    r.Tag,
                    r.Title,
                    r.Body,
                    r.Summary,
                    r.SummaryGeneratedAt,
                    r.PublishedAt,
                    r.FetchedAt,
                    Package = new
                    {
                        r.Package.Id,
                        r.Package.NpmName,
                        r.Package.GithubOwner,
                        r.Package.GithubRepo
                    }
                })
                .FirstOrDefaultAsync();

            if (release == null)
            {
                return Results.NotFound(new { error = "Release not found" });
            }

            return Results.Ok(release);
        });

        // GET /api/releases - Query releases for selected packages
        app.MapGet("/api/releases", async (string? packages, int? days, bool? excludePrerelease, int? majorVersion,
            bool? watchlist, HttpContext httpContext, PatchNotesDbContext db, IStytchClient stytchClient,
            IOptions<DefaultWatchlistOptions> watchlistOptions) =>
        {
            var daysToQuery = days ?? 7;
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToQuery);

            IQueryable<Release> query = db.Releases
                .Include(r => r.Package)
                .Where(r => r.PublishedAt >= cutoffDate);

            if (watchlist == true)
            {
                // Explicit watchlist filter — require authentication
                var sessionToken = httpContext.Request.Cookies["stytch_session"];
                if (string.IsNullOrEmpty(sessionToken))
                {
                    return Results.Json(new { error = "Authentication required for watchlist filter" }, statusCode: 401);
                }

                var session = await stytchClient.AuthenticateSessionAsync(sessionToken, httpContext.RequestAborted);
                if (session == null)
                {
                    return Results.Json(new { error = "Authentication required for watchlist filter" }, statusCode: 401);
                }

                var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == session.UserId);
                if (user == null)
                {
                    return Results.Json(new { error = "Authentication required for watchlist filter" }, statusCode: 401);
                }

                var watchlistIds = await db.Watchlists
                    .Where(w => w.UserId == user.Id)
                    .Select(w => w.PackageId)
                    .ToListAsync();

                query = query.Where(r => watchlistIds.Contains(r.PackageId));
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
                var watchlistIds = await ResolveWatchlistPackageIds(
                    httpContext, db, stytchClient, watchlistOptions.Value);
                if (watchlistIds.Count > 0)
                {
                    query = query.Where(r => watchlistIds.Contains(r.PackageId));
                }
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
                .Select(r => new
                {
                    r.Id,
                    r.Tag,
                    r.Title,
                    r.Body,
                    r.Summary,
                    r.SummaryGeneratedAt,
                    r.PublishedAt,
                    r.FetchedAt,
                    Package = new
                    {
                        r.Package.Id,
                        r.Package.NpmName,
                        r.Package.GithubOwner,
                        r.Package.GithubRepo
                    }
                })
                .ToListAsync();

            return Results.Ok(releases);
        });

        // POST /api/releases/{id}/summarize - Generate AI summary for a release
        app.MapPost("/api/releases/{id}/summarize", async (string id, HttpContext httpContext, PatchNotesDbContext db, IAiClient aiClient, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PatchNotes.Api.Routes.ReleaseRoutes");

            var release = await db.Releases
                .Include(r => r.Package)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (release == null)
            {
                return Results.NotFound(new { error = "Release not found" });
            }

            // Return cached summary if it exists and isn't stale
            if (!release.NeedsSummary)
            {
                return Results.Ok(new
                {
                    release.Id,
                    release.Tag,
                    release.Title,
                    summary = release.Summary,
                    Package = new
                    {
                        release.Package.Id,
                        release.Package.NpmName
                    }
                });
            }

            // Gather same-week siblings for richer context
            var releaseInputs = await BuildReleaseInputs(db, release);
            var packageName = release.Package.NpmName ?? release.Package.Name;

            var acceptHeader = httpContext.Request.Headers.Accept.ToString();

            // Support Server-Sent Events for streaming
            if (acceptHeader.Contains("text/event-stream"))
            {
                httpContext.Response.ContentType = "text/event-stream";
                httpContext.Response.Headers.CacheControl = "no-cache";
                httpContext.Response.Headers.Connection = "keep-alive";

                var fullSummary = new System.Text.StringBuilder();
                var eventId = 0;

                try
                {
                    await foreach (var chunk in aiClient.SummarizeReleaseNotesStreamAsync(packageName, releaseInputs, httpContext.RequestAborted))
                    {
                        fullSummary.Append(chunk);
                        var chunkData = JsonSerializer.Serialize(new { type = "chunk", content = chunk });
                        await httpContext.Response.WriteAsync($"id: {++eventId}\ndata: {chunkData}\n\n", httpContext.RequestAborted);
                        await httpContext.Response.Body.FlushAsync(httpContext.RequestAborted);
                    }

                    // Persist the generated summary with optimistic concurrency
                    release.Summary = fullSummary.ToString();
                    release.SummaryGeneratedAt = DateTime.UtcNow;
                    release.SummaryStale = false;
                    release.SummaryVersion = IdGenerator.NewId();
                    try
                    {
                        await db.SaveChangesAsync(httpContext.RequestAborted);
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        // Another request already persisted a summary - that's fine,
                        // the streamed chunks were already sent to the client
                        logger.LogInformation("Concurrent summary persistence for release {ReleaseId} - another request won the race", id);
                    }

                    var completeData = JsonSerializer.Serialize(new
                    {
                        type = "complete",
                        result = new
                        {
                            releaseId = release.Id,
                            release.Tag,
                            release.Title,
                            summary = release.Summary,
                            package = new { release.Package.Id, release.Package.NpmName }
                        }
                    });
                    await httpContext.Response.WriteAsync($"id: {++eventId}\ndata: {completeData}\n\n", httpContext.RequestAborted);
                }
                catch (OperationCanceledException)
                {
                    // Client disconnected - nothing to send
                    return Results.Empty;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "AI summarization failed for release {ReleaseId} during streaming", id);
                    var errorData = JsonSerializer.Serialize(new { type = "error", message = "AI summarization service is currently unavailable. Please try again later." });
                    await httpContext.Response.WriteAsync($"id: {++eventId}\ndata: {errorData}\n\n");
                }

                await httpContext.Response.WriteAsync($"id: {++eventId}\ndata: [DONE]\n\n");

                return Results.Empty;
            }

            // Non-streaming JSON response
            string summary;
            try
            {
                summary = await aiClient.SummarizeReleaseNotesAsync(packageName, releaseInputs);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI summarization failed for release {ReleaseId}", id);
                return Results.Problem(
                    detail: "AI summarization service is currently unavailable. Please try again later.",
                    statusCode: 503);
            }

            // Persist the generated summary with optimistic concurrency
            release.Summary = summary;
            release.SummaryGeneratedAt = DateTime.UtcNow;
            release.SummaryStale = false;
            release.SummaryVersion = IdGenerator.NewId();
            try
            {
                await db.SaveChangesAsync(httpContext.RequestAborted);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another request already persisted a summary — use locally-generated one.
                // ReloadAsync() is intentionally omitted: it can throw under SQLite concurrency
                // and release.Summary already holds a valid summary from the local generation above.
                logger.LogInformation("Concurrent summary persistence for release {ReleaseId} - returning locally generated summary", id);
            }

            return Results.Ok(new
            {
                release.Id,
                release.Tag,
                release.Title,
                summary = release.Summary,
                Package = new
                {
                    release.Package.Id,
                    release.Package.NpmName
                }
            });
        }).AddEndpointFilterFactory(requireAuth);

        return app;
    }

    /// <summary>
    /// Builds a list of ReleaseInputs for the target release plus any same-week siblings
    /// from the same package, ordered by PublishedAt descending.
    /// </summary>
    private static async Task<List<ReleaseInput>> BuildReleaseInputs(PatchNotesDbContext db, Release release)
    {
        var weekStart = release.PublishedAt.Date.AddDays(-(int)release.PublishedAt.DayOfWeek);
        var weekEnd = weekStart.AddDays(7);

        var siblings = await db.Releases
            .Where(r => r.PackageId == release.PackageId
                        && r.Id != release.Id
                        && r.PublishedAt >= weekStart
                        && r.PublishedAt < weekEnd)
            .OrderByDescending(r => r.PublishedAt)
            .ToListAsync();

        var allReleases = new List<Release> { release };
        allReleases.AddRange(siblings);

        return allReleases
            .OrderByDescending(r => r.PublishedAt)
            .Select(r => new ReleaseInput(r.Tag, r.Title, r.Body, r.PublishedAt))
            .ToList();
    }

    /// <summary>
    /// Resolves package IDs to filter by: user's watchlist if authenticated with a non-empty
    /// watchlist, otherwise the default watchlist from config.
    /// </summary>
    private static async Task<List<string>> ResolveWatchlistPackageIds(
        HttpContext httpContext, PatchNotesDbContext db,
        IStytchClient stytchClient, DefaultWatchlistOptions defaultWatchlist)
    {
        // Try optional auth — don't require it
        var sessionToken = httpContext.Request.Cookies["stytch_session"];
        if (!string.IsNullOrEmpty(sessionToken))
        {
            var session = await stytchClient.AuthenticateSessionAsync(sessionToken, httpContext.RequestAborted);
            if (session != null)
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == session.UserId);
                if (user != null)
                {
                    var watchlistIds = await db.Watchlists
                        .Where(w => w.UserId == user.Id)
                        .Select(w => w.PackageId)
                        .ToListAsync();

                    if (watchlistIds.Count > 0)
                    {
                        return watchlistIds;
                    }
                }
            }
        }

        // Fall back to default watchlist
        if (defaultWatchlist.Packages.Length == 0)
        {
            return [];
        }

        // Build separate owner/repo lists and use string concatenation for EF Core translation
        var ownerRepoPairs = defaultWatchlist.Packages
            .Select(p => p.Split('/'))
            .Where(parts => parts.Length == 2)
            .Select(parts => parts[0] + "/" + parts[1])
            .ToList();

        var defaultIds = await db.Packages
            .Where(p => ownerRepoPairs.Contains(p.GithubOwner + "/" + p.GithubRepo))
            .Select(p => p.Id)
            .ToListAsync();

        return defaultIds;
    }
}
