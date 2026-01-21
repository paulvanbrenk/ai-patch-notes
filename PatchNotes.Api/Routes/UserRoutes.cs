using Microsoft.EntityFrameworkCore;
using PatchNotes.Data;

namespace PatchNotes.Api.Routes;

public static class UserRoutes
{
    public static WebApplication MapUserRoutes(this WebApplication app)
    {
        // GET /api/users/me - Get current user by Stytch user ID
        app.MapGet("/api/users/me", async (string stytchUserId, PatchNotesDbContext db) =>
        {
            if (string.IsNullOrEmpty(stytchUserId))
            {
                return Results.BadRequest(new { error = "stytchUserId query parameter is required" });
            }

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
        });

        // POST /api/users/login - Create or update user on login (called from frontend)
        app.MapPost("/api/users/login", async (UserLoginRequest request, PatchNotesDbContext db) =>
        {
            if (string.IsNullOrEmpty(request.StytchUserId))
            {
                return Results.BadRequest(new { error = "stytchUserId is required" });
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == request.StytchUserId);

            if (user == null)
            {
                user = new User
                {
                    StytchUserId = request.StytchUserId,
                    Email = request.Email,
                    Name = request.Name,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };
                db.Users.Add(user);
            }
            else
            {
                user.Email = request.Email ?? user.Email;
                user.Name = request.Name ?? user.Name;
                user.UpdatedAt = DateTime.UtcNow;
                user.LastLoginAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                user.Id,
                user.StytchUserId,
                user.Email,
                user.Name,
                user.CreatedAt,
                user.LastLoginAt
            });
        });

        return app;
    }
}
