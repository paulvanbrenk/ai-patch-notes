using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PatchNotes.Data;
using PatchNotes.Api.Stytch;

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
            // CRITICAL: Fail early if webhook secret is not configured
            var stytchWebhookSecret = configuration["Stytch:WebhookSecret"];
            if (string.IsNullOrEmpty(stytchWebhookSecret))
            {
                Console.WriteLine("Stytch:WebhookSecret is not configured. Rejecting webhook to prevent unverified payloads");
                return Results.StatusCode(503);
            }

            // Read Svix headers for signature verification
            var svixId = httpContext.Request.Headers["svix-id"].FirstOrDefault();
            var svixTimestamp = httpContext.Request.Headers["svix-timestamp"].FirstOrDefault();
            var svixSignature = httpContext.Request.Headers["svix-signature"].FirstOrDefault();

            // Read the raw body for signature verification
            using var reader = new StreamReader(httpContext.Request.Body);
            var body = await reader.ReadToEndAsync();

            // Verify webhook signature using Svix
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

            // Step 1: Deserialize the webhook payload
            StytchWebhookEvent? stytchEvent;
            try
            {
                stytchEvent = JsonSerializer.Deserialize<StytchWebhookEvent>(body);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Stytch webhook JSON parse error: {ex.Message}");
                return Results.BadRequest(new { error = "Invalid webhook payload" });
            }

            if (stytchEvent == null)
            {
                return Results.BadRequest(new { error = "Invalid webhook payload" });
            }

            Console.WriteLine($"Stytch webhook received: object_type={stytchEvent.object_type}, action={stytchEvent.action}, id={stytchEvent.id}");

            // Handle different Stytch webhook events based on object_type and action
            // IMPORTANT: Don't trust data in the webhook payload - fetch fresh data from Stytch API
            switch (stytchEvent)
            {
                case { object_type: "user", action: "CREATE" or "UPDATE" }:
                    {
                        // Step 2: Fetch fresh user data from Stytch API
                        StytchUser? stytchUser;
                        try
                        {
                            stytchUser = await stytchClient.GetUserAsync(stytchEvent.id);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Stytch API call failed for user {stytchEvent.id}: {ex.Message}");
                            return Results.Json(new { error = "Failed to fetch user data" }, statusCode: 502);
                        }

                        if (stytchUser == null)
                        {
                            Console.WriteLine($"Stytch API returned null for user {stytchEvent.id}");
                            return Results.Json(new { error = "Failed to fetch user data" }, statusCode: 502);
                        }

                        Console.WriteLine($"Stytch user fetched: userId={stytchUser.UserId}, email={stytchUser.Email}, name={stytchUser.Name}");

                        // Step 3: Find or create user in DB
                        User? user;
                        try
                        {
                            user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchEvent.id);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"DB query failed for StytchUserId={stytchEvent.id}: {ex.Message}");
                            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
                        }

                        if (user == null)
                        {
                            Console.WriteLine($"Creating new user for StytchUserId={stytchEvent.id}");
                            user = new User
                            {
                                StytchUserId = stytchUser.UserId,
                                Email = stytchUser.Email,
                                Name = stytchUser.Name,
                            };
                            db.Users.Add(user);
                        }
                        else
                        {
                            Console.WriteLine($"Updating existing user {user.Id} for StytchUserId={stytchEvent.id}");
                            user.Email = stytchUser.Email ?? user.Email;
                            user.Name = stytchUser.Name ?? user.Name;
                        }

                        // Step 4: Save to DB
                        try
                        {
                            await db.SaveChangesAsync();
                            Console.WriteLine($"DB save succeeded for StytchUserId={stytchEvent.id}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"DB save failed for StytchUserId={stytchEvent.id}: {ex.Message} | Inner: {ex.InnerException?.Message}");
                            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
                        }

                        break;
                    }

                case { object_type: "user", action: "DELETE" }:
                    {
                        try
                        {
                            var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchEvent.id);
                            if (user != null)
                            {
                                db.Users.Remove(user);
                                await db.SaveChangesAsync();
                                Console.WriteLine($"Deleted user for StytchUserId={stytchEvent.id}");
                            }
                            else
                            {
                                Console.WriteLine($"Delete webhook: no user found for StytchUserId={stytchEvent.id}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Delete user failed for StytchUserId={stytchEvent.id}: {ex.Message}");
                            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
                        }
                        break;
                    }

                default:
                    Console.WriteLine($"Unhandled Stytch webhook: object_type={stytchEvent.object_type}, action={stytchEvent.action}");
                    break;
            }

            return Results.Ok(new { received = true });
        });

        return app;
    }
}
