using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PatchNotes.Data;

namespace PatchNotes.Api.Routes;

public static class UserRoutes
{
    public static WebApplication MapUserRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        // GET /api/users/me - Get current authenticated user
        group.MapGet("/me", async (HttpContext httpContext, PatchNotesDbContext db) =>
        {
            var stytchUserId = httpContext.Items["StytchUserId"] as string;

            var user = await db.Users
                .Where(u => u.StytchUserId == stytchUserId)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    StytchUserId = u.StytchUserId,
                    Email = u.Email,
                    Name = u.Name,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return Results.NotFound(new { error = "User not found" });
            }

            return Results.Ok(user);
        })
        .AddEndpointFilterFactory(RouteUtils.CreateAuthFilter())
        .Produces<UserDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName("GetCurrentUser");

        // POST /api/users/login - Create or update user on login (called from frontend after auth)
        group.MapPost("/login", async (
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
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    LastLoginAt = DateTimeOffset.UtcNow
                };
                db.Users.Add(user);
                await db.SaveChangesAsync();

                // Auto-populate watchlist with default packages (single batch query)
                var defaults = watchlistOptions.Value.Packages;
                if (defaults.Length > 0)
                {
                    var limit = user.IsPro ? defaults.Length : Math.Min(defaults.Length, WatchlistRoutes.FreeWatchlistLimit);

                    var ownerRepoPairs = defaults
                        .Select(p => p.Split('/', 2))
                        .Where(parts => parts.Length == 2)
                        .Select(parts => parts[0] + "/" + parts[1])
                        .ToList();

                    var matchingPackages = await db.Packages
                        .Where(p => ownerRepoPairs.Contains(p.GithubOwner + "/" + p.GithubRepo))
                        .ToListAsync();

                    foreach (var package in matchingPackages.Take(limit))
                    {
                        db.Watchlists.Add(new Watchlist
                        {
                            UserId = user.Id,
                            PackageId = package.Id,
                            CreatedAt = DateTimeOffset.UtcNow,
                        });
                    }

                    await db.SaveChangesAsync();
                }
            }
            else
            {
                user!.Email = email ?? user.Email;
                user.UpdatedAt = DateTimeOffset.UtcNow;
                user.LastLoginAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync();
            }

            return Results.Ok(new UserDto
            {
                Id = user.Id,
                StytchUserId = user.StytchUserId,
                Email = user.Email,
                Name = user.Name,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        })
        .AddEndpointFilterFactory(RouteUtils.CreateAuthFilter())
        .Produces<UserDto>(StatusCodes.Status200OK)
        .WithName("LoginUser");

        return app;
    }
}

public class UserDto
{
    public required string Id { get; set; }
    public required string StytchUserId { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}
