using Microsoft.EntityFrameworkCore;
using PatchNotes.Data;
using Stripe;
using Stripe.Checkout;

namespace PatchNotes.Api.Routes;

public static class SubscriptionRoutes
{
    public static WebApplication MapSubscriptionRoutes(this WebApplication app)
    {
        var requireAuth = RouteUtils.CreateAuthFilter();

        // POST /api/subscription/checkout - Create Stripe Checkout session
        app.MapPost("/api/subscription/checkout", async (HttpContext httpContext, PatchNotesDbContext db, IConfiguration configuration) =>
        {
            var stytchUserId = httpContext.Items["StytchUserId"] as string;
            if (string.IsNullOrEmpty(stytchUserId))
            {
                return Results.Unauthorized();
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchUserId);
            if (user == null)
            {
                return Results.NotFound(new { error = "User not found" });
            }

            var priceId = configuration["Stripe:PriceId"];
            if (string.IsNullOrEmpty(priceId))
            {
                return Results.Problem("Stripe is not configured");
            }

            // Determine the base URL for success/cancel redirects
            var origin = httpContext.Request.Headers.Origin.FirstOrDefault()
                ?? $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

            var sessionOptions = new SessionCreateOptions
            {
                Mode = "subscription",
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Price = priceId,
                        Quantity = 1,
                    },
                },
                SuccessUrl = $"{origin}/subscription-success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{origin}/subscription-canceled",
                CustomerEmail = user.Email,
                Metadata = new Dictionary<string, string>
                {
                    { "stytch_user_id", stytchUserId },
                    { "app", "patchnotes" },
                },
            };

            // If user already has a Stripe customer ID, use it
            if (!string.IsNullOrEmpty(user.StripeCustomerId))
            {
                sessionOptions.Customer = user.StripeCustomerId;
                sessionOptions.CustomerEmail = null; // Can't use both
            }

            var sessionService = new SessionService();
            var session = await sessionService.CreateAsync(sessionOptions);

            return Results.Ok(new { url = session.Url });
        }).AddEndpointFilterFactory(requireAuth);

        // POST /api/subscription/portal - Create Stripe Customer Portal session
        app.MapPost("/api/subscription/portal", async (HttpContext httpContext, PatchNotesDbContext db) =>
        {
            var stytchUserId = httpContext.Items["StytchUserId"] as string;
            if (string.IsNullOrEmpty(stytchUserId))
            {
                return Results.Unauthorized();
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchUserId);
            if (user == null)
            {
                return Results.NotFound(new { error = "User not found" });
            }

            if (string.IsNullOrEmpty(user.StripeCustomerId))
            {
                return Results.BadRequest(new { error = "No subscription found" });
            }

            var origin = httpContext.Request.Headers.Origin.FirstOrDefault()
                ?? $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

            var portalOptions = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = user.StripeCustomerId,
                ReturnUrl = $"{origin}/",
            };

            var portalService = new Stripe.BillingPortal.SessionService();
            var session = await portalService.CreateAsync(portalOptions);

            return Results.Ok(new { url = session.Url });
        }).AddEndpointFilterFactory(requireAuth);

        // GET /api/subscription/status - Get current subscription status
        app.MapGet("/api/subscription/status", async (HttpContext httpContext, PatchNotesDbContext db) =>
        {
            var stytchUserId = httpContext.Items["StytchUserId"] as string;
            if (string.IsNullOrEmpty(stytchUserId))
            {
                return Results.Unauthorized();
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchUserId);
            if (user == null)
            {
                return Results.NotFound(new { error = "User not found" });
            }

            var isPro = user.SubscriptionStatus == "active" ||
                (user.SubscriptionStatus == "canceled" && user.SubscriptionExpiresAt > DateTime.UtcNow);

            return Results.Ok(new
            {
                isPro,
                status = user.SubscriptionStatus,
                expiresAt = user.SubscriptionExpiresAt?.ToString("o"),
            });
        }).AddEndpointFilterFactory(requireAuth);

        return app;
    }
}
