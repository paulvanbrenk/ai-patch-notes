using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PatchNotes.Data;
using PatchNotes.Data.AI;

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
        app.MapGet("/api/releases", async (string? packages, int? days, bool? excludePrerelease, int? majorVersion, PatchNotesDbContext db) =>
        {
            var daysToQuery = days ?? 7;
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToQuery);

            IQueryable<Release> query = db.Releases
                .Include(r => r.Package)
                .Where(r => r.PublishedAt >= cutoffDate);

            if (!string.IsNullOrEmpty(packages))
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

            var acceptHeader = httpContext.Request.Headers.Accept.ToString();

            // Support Server-Sent Events for streaming
            if (acceptHeader.Contains("text/event-stream"))
            {
                httpContext.Response.ContentType = "text/event-stream";
                httpContext.Response.Headers.CacheControl = "no-cache";
                httpContext.Response.Headers.Connection = "keep-alive";

                var fullSummary = new System.Text.StringBuilder();

                await foreach (var chunk in aiClient.SummarizeReleaseNotesStreamAsync(release.Title, release.Body, httpContext.RequestAborted))
                {
                    fullSummary.Append(chunk);
                    var chunkData = JsonSerializer.Serialize(new { type = "chunk", content = chunk });
                    await httpContext.Response.WriteAsync($"data: {chunkData}\n\n", httpContext.RequestAborted);
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
                    loggerFactory.CreateLogger("PatchNotes.Api.Routes.ReleaseRoutes").LogInformation("Concurrent summary persistence for release {ReleaseId} - another request won the race", id);
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
                await httpContext.Response.WriteAsync($"data: {completeData}\n\n", httpContext.RequestAborted);
                await httpContext.Response.WriteAsync("data: [DONE]\n\n", httpContext.RequestAborted);

                return Results.Empty;
            }

            // Non-streaming JSON response
            string summary;
            try
            {
                summary = await aiClient.SummarizeReleaseNotesAsync(release.Title, release.Body);
            }
            catch (Exception ex)
            {
                loggerFactory.CreateLogger("PatchNotes.Api.Routes.ReleaseRoutes")
                    .LogError(ex, "AI summarization failed for release {ReleaseId}", id);
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
                // Another request already persisted a summary - return it instead
                loggerFactory.CreateLogger("PatchNotes.Api.Routes.ReleaseRoutes").LogInformation("Concurrent summary persistence for release {ReleaseId} - returning winner's summary", id);
                await db.Entry(release).ReloadAsync();
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
}
