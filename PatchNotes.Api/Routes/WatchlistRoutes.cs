using Microsoft.EntityFrameworkCore;
using PatchNotes.Data;

namespace PatchNotes.Api.Routes;

public static class WatchlistRoutes
{
    internal const int FreeWatchlistLimit = 5;
    private const int MaxWatchlistSize = 1000;

    public static WebApplication MapWatchlistRoutes(this WebApplication app)
    {
        var requireAuth = RouteUtils.CreateAuthFilter();

        var group = app.MapGroup("/api/watchlist").WithTags("Watchlist");

        // GET /api/watchlist — return list of package IDs the current user is watching
        group.MapGet("/", async (HttpContext httpContext, PatchNotesDbContext db) =>
        {
            var stytchUserId = httpContext.Items["StytchUserId"] as string;
            if (stytchUserId == null)
            {
                return Results.Unauthorized();
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchUserId);
            if (user == null)
            {
                return Results.Ok(Array.Empty<string>());
            }

            var packageIds = await db.Watchlists
                .Where(w => w.UserId == user.Id)
                .Select(w => w.PackageId)
                .ToArrayAsync();

            return Results.Ok(packageIds);
        })
        .AddEndpointFilterFactory(requireAuth)
        .Produces<string[]>(StatusCodes.Status200OK)
        .WithName("GetWatchlist");

        // PUT /api/watchlist — replace the entire watchlist (bulk set)
        group.MapPut("/", async (SetWatchlistRequest request, HttpContext httpContext, PatchNotesDbContext db) =>
        {
            var stytchUserId = httpContext.Items["StytchUserId"] as string;
            if (stytchUserId == null)
            {
                return Results.Unauthorized();
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchUserId);
            if (user == null)
            {
                return Results.NotFound(new { error = "User not found" });
            }

            var packageIds = request.PackageIds ?? [];

            if (!user.IsPro && packageIds.Length > FreeWatchlistLimit)
            {
                return Results.Json(new { error = $"Free plan is limited to {FreeWatchlistLimit} packages. Upgrade to Pro for unlimited." }, statusCode: 403);
            }
            if (packageIds.Length > MaxWatchlistSize)
            {
                return Results.BadRequest(new { error = $"Watchlist cannot exceed {MaxWatchlistSize} packages" });
            }

            var distinctIds = packageIds.Distinct().ToArray();
            var existingPackageCount = await db.Packages
                .Where(p => distinctIds.Contains(p.Id))
                .CountAsync();
            if (existingPackageCount != distinctIds.Length)
            {
                return Results.BadRequest(new { error = "One or more package IDs do not exist" });
            }

            var existing = await db.Watchlists
                .Where(w => w.UserId == user.Id)
                .ToListAsync();
            db.Watchlists.RemoveRange(existing);

            foreach (var packageId in distinctIds)
            {
                db.Watchlists.Add(new Watchlist
                {
                    UserId = user.Id,
                    PackageId = packageId,
                });
            }

            await db.SaveChangesAsync();

            var resultIds = await db.Watchlists
                .Where(w => w.UserId == user.Id)
                .Select(w => w.PackageId)
                .ToArrayAsync();

            return Results.Ok(resultIds);
        })
        .AddEndpointFilterFactory(requireAuth)
        .Produces<string[]>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .WithName("SetWatchlist");

        // POST /api/watchlist/{packageId} — add a single package to watchlist
        group.MapPost("/{packageId}", async (string packageId, HttpContext httpContext, PatchNotesDbContext db) =>
        {
            var stytchUserId = httpContext.Items["StytchUserId"] as string;
            if (stytchUserId == null)
            {
                return Results.Unauthorized();
            }

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

            var watchlistSize = await db.Watchlists.CountAsync(w => w.UserId == user.Id);
            if (!user.IsPro && watchlistSize >= FreeWatchlistLimit)
            {
                return Results.Json(new { error = $"Free plan is limited to {FreeWatchlistLimit} packages. Upgrade to Pro for unlimited." }, statusCode: 403);
            }
            if (watchlistSize >= MaxWatchlistSize)
            {
                return Results.BadRequest(new { error = $"Watchlist cannot exceed {MaxWatchlistSize} packages" });
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
            });
            await db.SaveChangesAsync();

            return Results.Created($"/api/watchlist/{packageId}", packageId);
        })
        .AddEndpointFilterFactory(requireAuth)
        .Produces<string>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .WithName("AddToWatchlist");

        // DELETE /api/watchlist/{packageId} — remove a single package from watchlist
        group.MapDelete("/{packageId}", async (string packageId, HttpContext httpContext, PatchNotesDbContext db) =>
        {
            var stytchUserId = httpContext.Items["StytchUserId"] as string;
            if (stytchUserId == null)
            {
                return Results.Unauthorized();
            }

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
        })
        .AddEndpointFilterFactory(requireAuth)
        .Produces(StatusCodes.Status204NoContent)
        .WithName("RemoveFromWatchlist");

        return app;
    }
}

public record SetWatchlistRequest(string[] PackageIds);
