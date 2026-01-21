using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PatchNotes.Data;

namespace PatchNotes.Api.Webhooks;

public static class StytchWebhook
{
    public static WebApplication MapStytchWebhook(this WebApplication app)
    {
        // POST /api/webhooks/stytch - Handle Stytch webhook events
        app.MapPost("/api/webhooks/stytch", async (HttpContext httpContext, PatchNotesDbContext db, IConfiguration configuration) =>
        {
            var stytchWebhookSecret = configuration["Stytch:WebhookSecret"];

            // Read the raw body for signature verification
            using var reader = new StreamReader(httpContext.Request.Body);
            var body = await reader.ReadToEndAsync();

            // In production, verify the webhook signature
            // See: https://stytch.com/docs/api/webhooks
            if (!string.IsNullOrEmpty(stytchWebhookSecret))
            {
                var signature = httpContext.Request.Headers["X-Stytch-Signature"].FirstOrDefault();
                if (string.IsNullOrEmpty(signature))
                {
                    return Results.Unauthorized();
                }

                // Verify HMAC-SHA256 signature
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(stytchWebhookSecret));
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
                var computedSignature = Convert.ToBase64String(computedHash);

                if (signature != computedSignature)
                {
                    return Results.Unauthorized();
                }
            }

            var payload = JsonDocument.Parse(body);
            var root = payload.RootElement;

            if (!root.TryGetProperty("event_type", out var eventTypeElement))
            {
                return Results.BadRequest(new { error = "Missing event_type" });
            }

            var eventType = eventTypeElement.GetString();

            // Handle different Stytch webhook events
            switch (eventType)
            {
                case "user.created":
                case "user.updated":
                    if (root.TryGetProperty("data", out var data) &&
                        data.TryGetProperty("user_id", out var userIdElement))
                    {
                        var stytchUserId = userIdElement.GetString();
                        if (string.IsNullOrEmpty(stytchUserId)) break;

                        string? email = null;
                        string? name = null;

                        // Extract email from emails array
                        if (data.TryGetProperty("emails", out var emails) &&
                            emails.ValueKind == JsonValueKind.Array &&
                            emails.GetArrayLength() > 0)
                        {
                            var firstEmail = emails[0];
                            if (firstEmail.TryGetProperty("email", out var emailElement))
                            {
                                email = emailElement.GetString();
                            }
                        }

                        // Extract name if available
                        if (data.TryGetProperty("name", out var nameObj))
                        {
                            var firstName = nameObj.TryGetProperty("first_name", out var fn) ? fn.GetString() : null;
                            var lastName = nameObj.TryGetProperty("last_name", out var ln) ? ln.GetString() : null;
                            name = string.Join(" ", new[] { firstName, lastName }.Where(n => !string.IsNullOrEmpty(n)));
                            if (string.IsNullOrEmpty(name)) name = null;
                        }

                        var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchUserId);

                        if (user == null)
                        {
                            user = new User
                            {
                                StytchUserId = stytchUserId,
                                Email = email,
                                Name = name,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            db.Users.Add(user);
                        }
                        else
                        {
                            user.Email = email ?? user.Email;
                            user.Name = name ?? user.Name;
                            user.UpdatedAt = DateTime.UtcNow;
                        }

                        await db.SaveChangesAsync();
                    }
                    break;

                case "user.deleted":
                    if (root.TryGetProperty("data", out var deleteData) &&
                        deleteData.TryGetProperty("user_id", out var deleteUserIdElement))
                    {
                        var stytchUserId = deleteUserIdElement.GetString();
                        if (!string.IsNullOrEmpty(stytchUserId))
                        {
                            var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchUserId);
                            if (user != null)
                            {
                                db.Users.Remove(user);
                                await db.SaveChangesAsync();
                            }
                        }
                    }
                    break;

                default:
                    // Log unknown event types but don't fail
                    Console.WriteLine($"Received unknown Stytch webhook event: {eventType}");
                    break;
            }

            return Results.Ok(new { received = true });
        });

        return app;
    }
}
