using Microsoft.EntityFrameworkCore;
using PatchNotes.Data;

namespace PatchNotes.Api.Routes;

public static class SummaryRoutes
{
    public static WebApplication MapSummaryRoutes(this WebApplication app)
    {
        // GET /api/packages/{id}/summaries - Get all release summaries for a package
        app.MapGet("/api/packages/{id}/summaries", async (
            string id,
            bool? includePrerelease,
            int? majorVersion,
            PatchNotesDbContext db) =>
        {
            var packageExists = await db.Packages.AnyAsync(p => p.Id == id);
            if (!packageExists)
            {
                return Results.NotFound(new { error = "Package not found" });
            }

            IQueryable<ReleaseSummary> query = db.ReleaseSummaries
                .Include(s => s.Package)
                .Where(s => s.PackageId == id);

            if (includePrerelease != true)
            {
                query = query.Where(s => !s.IsPrerelease);
            }

            if (majorVersion.HasValue)
            {
                query = query.Where(s => s.MajorVersion == majorVersion.Value);
            }

            var summaries = await query
                .OrderByDescending(s => s.MajorVersion)
                .Select(s => new
                {
                    s.Id,
                    s.PackageId,
                    PackageName = s.Package.Name,
                    s.MajorVersion,
                    s.IsPrerelease,
                    s.Summary,
                    s.GeneratedAt,
                    s.UpdatedAt
                })
                .ToListAsync();

            return Results.Ok(summaries);
        });

        // GET /api/summaries - Get summaries across all packages (or filtered)
        app.MapGet("/api/summaries", async (
            string? packages,
            bool? includePrerelease,
            int? limit,
            PatchNotesDbContext db) =>
        {
            var take = limit ?? 20;

            IQueryable<ReleaseSummary> query = db.ReleaseSummaries
                .Include(s => s.Package);

            if (!string.IsNullOrEmpty(packages))
            {
                var packageIds = packages.Split(',')
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToList();

                if (packageIds.Count > 0)
                {
                    query = query.Where(s => packageIds.Contains(s.PackageId));
                }
            }

            if (includePrerelease != true)
            {
                query = query.Where(s => !s.IsPrerelease);
            }

            var summaries = await query
                .OrderByDescending(s => s.MajorVersion)
                .Take(take)
                .Select(s => new
                {
                    s.Id,
                    s.PackageId,
                    PackageName = s.Package.Name,
                    s.MajorVersion,
                    s.IsPrerelease,
                    s.Summary,
                    s.GeneratedAt,
                    s.UpdatedAt
                })
                .ToListAsync();

            return Results.Ok(summaries);
        });

        return app;
    }
}
