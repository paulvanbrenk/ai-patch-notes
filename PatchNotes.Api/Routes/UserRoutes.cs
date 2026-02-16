using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PatchNotes.Data;
using PatchNotes.Api.Stytch;

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
                return Results.NotFound(new ApiError("User not found"));
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
                        });
                    }

                    await db.SaveChangesAsync();
                }
            }
            else
            {
                user!.Email = email ?? user.Email;
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

        // PUT /api/users/me - Update current user profile
        // Mirrors the webhook path: fetches user from Stytch API, then updates DB
        group.MapPut("/me", async (HttpContext httpContext, UpdateUserRequest request, PatchNotesDbContext db, IStytchClient stytchClient, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PatchNotes.Api.Routes.UserRoutes");
            var stytchUserId = httpContext.Items["StytchUserId"] as string;

            // Step 1: Fetch fresh user data from Stytch (same as webhook)
            StytchUser? stytchUser = null;
            try
            {
                stytchUser = await stytchClient.GetUserAsync(stytchUserId!);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stytch API call failed for user {StytchUserId}", stytchUserId);
                return Results.Json(new ApiError("Stytch API call failed"), statusCode: 502);
            }

            if (stytchUser == null)
            {
                return Results.Json(new ApiError("Could not fetch user from Stytch"), statusCode: 502);
            }

            // Step 2: Find user in DB (same as webhook)
            User? user;
            try
            {
                user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchUserId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DB query failed for user {StytchUserId}", stytchUserId);
                return Results.Json(new ApiError("DB query failed"), statusCode: 500);
            }

            if (user == null)
            {
                return Results.NotFound(new ApiError("User not found"));
            }

            // Step 3: Update fields (same as webhook, plus the name from request)
            try
            {
                user.Email = stytchUser.Email ?? user.Email;
                user.Name = request.Name ?? stytchUser.Name ?? user.Name;
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "DB save failed for user {StytchUserId}", stytchUserId);
                return Results.Json(new ApiError("DB save failed"), statusCode: 500);
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
        .Accepts<UpdateUserRequest>("application/json")
        .Produces<UserDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName("UpdateCurrentUser");

        // GET /api/users/me/email-preferences - Get current email preferences
        group.MapGet("/me/email-preferences", async (HttpContext httpContext, PatchNotesDbContext db) =>
        {
            var stytchUserId = httpContext.Items["StytchUserId"] as string;

            var prefs = await db.Users
                .Where(u => u.StytchUserId == stytchUserId)
                .Select(u => new EmailPreferencesDto
                {
                    EmailDigestEnabled = u.EmailDigestEnabled,
                    EmailWelcomeSent = u.EmailWelcomeSent
                })
                .FirstOrDefaultAsync();

            if (prefs == null)
            {
                return Results.NotFound(new ApiError("User not found"));
            }

            return Results.Ok(prefs);
        })
        .AddEndpointFilterFactory(RouteUtils.CreateAuthFilter())
        .Produces<EmailPreferencesDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName("GetEmailPreferences");

        // PATCH /api/users/me/email-preferences - Update email preferences
        group.MapPatch("/me/email-preferences", async (HttpContext httpContext, UpdateEmailPreferencesRequest request, PatchNotesDbContext db) =>
        {
            var stytchUserId = httpContext.Items["StytchUserId"] as string;

            var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchUserId);
            if (user == null)
            {
                return Results.NotFound(new ApiError("User not found"));
            }

            if (request.EmailDigestEnabled.HasValue)
                user.EmailDigestEnabled = request.EmailDigestEnabled.Value;

            await db.SaveChangesAsync();

            return Results.Ok(new EmailPreferencesDto
            {
                EmailDigestEnabled = user.EmailDigestEnabled,
                EmailWelcomeSent = user.EmailWelcomeSent
            });
        })
        .AddEndpointFilterFactory(RouteUtils.CreateAuthFilter())
        .Accepts<UpdateEmailPreferencesRequest>("application/json")
        .Produces<EmailPreferencesDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName("UpdateEmailPreferences");

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

public record UpdateUserRequest(string? Name);

public class EmailPreferencesDto
{
    public bool EmailDigestEnabled { get; set; }
    public bool EmailWelcomeSent { get; set; }
}

public record UpdateEmailPreferencesRequest(bool? EmailDigestEnabled);
