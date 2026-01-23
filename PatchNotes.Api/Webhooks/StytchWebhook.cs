using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PatchNotes.Data;
using PatchNotes.Data.Stytch;

namespace PatchNotes.Api.Webhooks;

/// <summary>
/// Webhook event from Stytch (via Svix).
/// </summary>
public record StytchWebhookEvent(
    string action,
    string id,
    string object_type
);

public static class StytchWebhook
{
    public static WebApplication MapStytchWebhook(this WebApplication app)
    {
        // POST /webhooks/stytch - Handle Stytch webhook events
        app.MapPost("/webhooks/stytch", async (HttpContext httpContext, PatchNotesDbContext db, IStytchClient stytchClient, IConfiguration configuration) =>
        {
            var stytchWebhookSecret = configuration["Stytch:WebhookSecret"];

            // Read Svix headers for signature verification
            var svixId = httpContext.Request.Headers["svix-id"].FirstOrDefault();
            var svixTimestamp = httpContext.Request.Headers["svix-timestamp"].FirstOrDefault();
            var svixSignature = httpContext.Request.Headers["svix-signature"].FirstOrDefault();

            // Read the raw body for signature verification
            using var reader = new StreamReader(httpContext.Request.Body);
            var body = await reader.ReadToEndAsync();

            // Verify webhook signature using Svix
            if (!string.IsNullOrEmpty(stytchWebhookSecret))
            {
                if (string.IsNullOrEmpty(svixId) || string.IsNullOrEmpty(svixTimestamp) || string.IsNullOrEmpty(svixSignature))
                {
                    return Results.Unauthorized();
                }

                var svix = new Svix.Webhook(stytchWebhookSecret);
                var headers = new WebHeaderCollection();
                headers.Set("svix-id", svixId);
                headers.Set("svix-timestamp", svixTimestamp);
                headers.Set("svix-signature", svixSignature);

                try
                {
                    svix.Verify(body, headers);
                }
                catch
                {
                    return Results.Unauthorized();
                }
            }

            try
            {
                var stytchEvent = JsonSerializer.Deserialize<StytchWebhookEvent>(body);

                if (stytchEvent == null)
                {
                    return Results.BadRequest(new { error = "Invalid webhook payload" });
                }

                // Handle different Stytch webhook events based on object_type and action
                // IMPORTANT: Don't trust data in the webhook payload - fetch fresh data from Stytch API
                switch (stytchEvent)
                {
                    case { object_type: "user", action: "created" or "updated" }:
                        {
                            // Fetch fresh user data from Stytch API (webhook data may be stale)
                            var stytchUser = await stytchClient.GetUserAsync(stytchEvent.id);

                            if (stytchUser == null)
                            {
                                Console.WriteLine($"Failed to fetch user {stytchEvent.id} from Stytch API");
                                // Return OK to acknowledge receipt - Stytch will retry if we fail
                                return Results.Ok(new { received = true, warning = "Could not fetch user data" });
                            }

                            var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchEvent.id);

                            if (user == null)
                            {
                                user = new User
                                {
                                    StytchUserId = stytchUser.UserId,
                                    Email = stytchUser.Email,
                                    Name = stytchUser.Name,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };
                                db.Users.Add(user);
                            }
                            else
                            {
                                user.Email = stytchUser.Email ?? user.Email;
                                user.Name = stytchUser.Name ?? user.Name;
                                user.UpdatedAt = DateTime.UtcNow;
                            }

                            await db.SaveChangesAsync();
                            break;
                        }

                    case { object_type: "user", action: "deleted" }:
                        {
                            var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchEvent.id);
                            if (user != null)
                            {
                                db.Users.Remove(user);
                                await db.SaveChangesAsync();
                            }
                            break;
                        }

                    default:
                        // Log unknown event types but don't fail
                        Console.WriteLine($"Received Stytch webhook: object_type={stytchEvent.object_type}, action={stytchEvent.action}");
                        break;
                }

                return Results.Ok(new { received = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Stytch webhook error: {ex.Message}");
                return Results.BadRequest(new { error = "Invalid webhook payload" });
            }
        });

        return app;
    }
}
