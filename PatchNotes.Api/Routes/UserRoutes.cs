using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PatchNotes.Data;

namespace PatchNotes.Api.Routes;

public static class UserRoutes
{
    public static WebApplication MapUserRoutes(this WebApplication app)
    {
        // GET /api/users/me - Get current authenticated user
        app.MapGet("/api/users/me", async (HttpContext httpContext, PatchNotesDbContext db) =>
        {
            var stytchUserId = httpContext.Items["StytchUserId"] as string;

            var user = await db.Users
                .Where(u => u.StytchUserId == stytchUserId)
                .Select(u => new
                {
                    u.Id,
                    u.StytchUserId,
                    u.Email,
                    u.Name,
                    u.CreatedAt,
                    u.LastLoginAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return Results.NotFound(new { error = "User not found" });
            }

            return Results.Ok(user);
        })
        .AddEndpointFilterFactory(RouteUtils.CreateAuthFilter());

        // POST /api/users/login - Create or update user on login (called from frontend after auth)
        app.MapPost("/api/users/login", async (
            HttpContext httpContext,
            PatchNotesDbContext db,
            IOptions<DefaultWatchlistOptions> watchlistOptions) =>
        {
            var stytchUserId = httpContext.Items["StytchUserId"] as string;
            var email = httpContext.Items["StytchEmail"] as string;

            var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchUserId);
            var isNewUser = user == null;

            if (isNewUser)
            {
                user = new User
                {
                    StytchUserId = stytchUserId!,
                    Email = email,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };
                db.Users.Add(user);
                await db.SaveChangesAsync();

                // Auto-populate watchlist with default packages
                var defaults = watchlistOptions.Value.Packages;
                if (defaults.Length > 0)
                {
                    var limit = user.IsPro ? defaults.Length : Math.Min(defaults.Length, WatchlistRoutes.FreeWatchlistLimit);
                    var added = 0;

                    foreach (var fullName in defaults)
                    {
                        if (added >= limit) break;

                        var parts = fullName.Split('/', 2);
                        if (parts.Length != 2) continue;

                        var package = await db.Packages
                            .FirstOrDefaultAsync(p => p.GithubOwner == parts[0] && p.GithubRepo == parts[1]);
                        if (package == null) continue;

                        db.Watchlists.Add(new Watchlist
                        {
                            UserId = user.Id,
                            PackageId = package.Id,
                            CreatedAt = DateTime.UtcNow,
                        });
                        added++;
                    }

                    await db.SaveChangesAsync();
                }
            }
            else
            {
                user!.Email = email ?? user.Email;
                user.UpdatedAt = DateTime.UtcNow;
                user.LastLoginAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            return Results.Ok(new
            {
                user.Id,
                user.StytchUserId,
                user.Email,
                user.Name,
                user.CreatedAt,
                user.LastLoginAt
            });
        })
        .AddEndpointFilterFactory(RouteUtils.CreateAuthFilter());

        return app;
    }
}
