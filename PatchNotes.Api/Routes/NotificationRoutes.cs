using Microsoft.EntityFrameworkCore;
using PatchNotes.Data;

namespace PatchNotes.Api.Routes;

public static class NotificationRoutes
{
    public static WebApplication MapNotificationRoutes(this WebApplication app)
    {
        var requireAuth = RouteUtils.CreateAuthFilter();

        // GET /api/notifications - Query notifications
        app.MapGet("/api/notifications", async (bool? unreadOnly, string? packageId, PatchNotesDbContext db) =>
        {
            IQueryable<Notification> query = db.Notifications
                .Include(n => n.Package);

            if (unreadOnly == true)
            {
                query = query.Where(n => n.Unread);
            }

            if (!string.IsNullOrEmpty(packageId))
            {
                query = query.Where(n => n.PackageId == packageId);
            }

            var notifications = await query
                .OrderByDescending(n => n.UpdatedAt)
                .Select(n => new
                {
                    n.Id,
                    n.GitHubId,
                    n.Reason,
                    n.SubjectTitle,
                    n.SubjectType,
                    n.SubjectUrl,
                    n.RepositoryFullName,
                    n.Unread,
                    n.UpdatedAt,
                    n.LastReadAt,
                    n.FetchedAt,
                    Package = n.Package == null ? null : new
                    {
                        n.Package.Id,
                        n.Package.NpmName,
                        n.Package.GithubOwner,
                        n.Package.GithubRepo
                    }
                })
                .ToListAsync();

            return Results.Ok(notifications);
        }).AddEndpointFilterFactory(requireAuth);

        // GET /api/notifications/unread-count - Get count of unread notifications
        app.MapGet("/api/notifications/unread-count", async (PatchNotesDbContext db) =>
        {
            var count = await db.Notifications.CountAsync(n => n.Unread);
            return Results.Ok(new { count });
        }).AddEndpointFilterFactory(requireAuth);

        // PATCH /api/notifications/{id}/read - Mark notification as read
        app.MapPatch("/api/notifications/{id}/read", async (string id, PatchNotesDbContext db) =>
        {
            var notification = await db.Notifications.FindAsync(id);
            if (notification == null)
            {
                return Results.NotFound(new { error = "Notification not found" });
            }

            notification.Unread = false;
            notification.LastReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new { notification.Id, notification.Unread, notification.LastReadAt });
        }).AddEndpointFilterFactory(requireAuth);

        // DELETE /api/notifications/{id} - Delete a notification
        app.MapDelete("/api/notifications/{id}", async (string id, PatchNotesDbContext db) =>
        {
            var notification = await db.Notifications.FindAsync(id);
            if (notification == null)
            {
                return Results.NotFound(new { error = "Notification not found" });
            }

            db.Notifications.Remove(notification);
            await db.SaveChangesAsync();

            return Results.NoContent();
        }).AddEndpointFilterFactory(requireAuth);

        return app;
    }
}
