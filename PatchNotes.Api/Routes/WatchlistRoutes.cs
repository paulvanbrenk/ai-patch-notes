using Microsoft.EntityFrameworkCore;
using PatchNotes.Data;

namespace PatchNotes.Api.Routes;

public static class WatchlistRoutes
{
    public static WebApplication MapWatchlistRoutes(this WebApplication app)
    {
        var requireAuth = RouteUtils.CreateAuthFilter();

        // GET /api/watchlist — return list of package IDs the current user is watching
        app.MapGet("/api/watchlist", async (HttpContext httpContext, PatchNotesDbContext db) =>
        {
            var stytchUserId = httpContext.Items["StytchUserId"] as string;
            var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchUserId);
            if (user == null)
            {
                return Results.Ok(Array.Empty<int>());
            }

            var packageIds = await db.Watchlists
                .Where(w => w.UserId == user.Id)
                .Select(w => w.PackageId)
                .ToArrayAsync();

            return Results.Ok(packageIds);
        }).AddEndpointFilterFactory(requireAuth);

        // PUT /api/watchlist — replace the entire watchlist (bulk set)
        app.MapPut("/api/watchlist", async (SetWatchlistRequest request, HttpContext httpContext, PatchNotesDbContext db) =>
        {
            var stytchUserId = httpContext.Items["StytchUserId"] as string;
            var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchUserId);
            if (user == null)
            {
                return Results.NotFound(new { error = "User not found" });
            }

            var existing = await db.Watchlists
                .Where(w => w.UserId == user.Id)
                .ToListAsync();
            db.Watchlists.RemoveRange(existing);

            var packageIds = request.PackageIds ?? [];
            foreach (var packageId in packageIds)
            {
                db.Watchlists.Add(new Watchlist
                {
                    UserId = user.Id,
                    PackageId = packageId,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            await db.SaveChangesAsync();

            var resultIds = await db.Watchlists
                .Where(w => w.UserId == user.Id)
                .Select(w => w.PackageId)
                .ToArrayAsync();

            return Results.Ok(resultIds);
        }).AddEndpointFilterFactory(requireAuth);

        // POST /api/watchlist/{packageId} — add a single package to watchlist
        app.MapPost("/api/watchlist/{packageId:int}", async (int packageId, HttpContext httpContext, PatchNotesDbContext db) =>
        {
            var stytchUserId = httpContext.Items["StytchUserId"] as string;
            var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchUserId);
            if (user == null)
            {
                return Results.NotFound(new { error = "User not found" });
            }

            var packageExists = await db.Packages.AnyAsync(p => p.Id == packageId);
            if (!packageExists)
            {
                return Results.NotFound(new { error = "Package not found" });
            }

            var alreadyWatching = await db.Watchlists
                .AnyAsync(w => w.UserId == user.Id && w.PackageId == packageId);
            if (alreadyWatching)
            {
                return Results.Conflict(new { error = "Already watching this package" });
            }

            db.Watchlists.Add(new Watchlist
            {
                UserId = user.Id,
                PackageId = packageId,
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();

            return Results.Created($"/api/watchlist/{packageId}", packageId);
        }).AddEndpointFilterFactory(requireAuth);

        // DELETE /api/watchlist/{packageId} — remove a single package from watchlist
        app.MapDelete("/api/watchlist/{packageId:int}", async (int packageId, HttpContext httpContext, PatchNotesDbContext db) =>
        {
            var stytchUserId = httpContext.Items["StytchUserId"] as string;
            var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchUserId);
            if (user == null)
            {
                return Results.NoContent();
            }

            var entry = await db.Watchlists
                .FirstOrDefaultAsync(w => w.UserId == user.Id && w.PackageId == packageId);
            if (entry != null)
            {
                db.Watchlists.Remove(entry);
                await db.SaveChangesAsync();
            }

            return Results.NoContent();
        }).AddEndpointFilterFactory(requireAuth);

        return app;
    }
}

public record SetWatchlistRequest(int[] PackageIds);
