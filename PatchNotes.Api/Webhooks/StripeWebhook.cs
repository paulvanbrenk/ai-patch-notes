using Microsoft.EntityFrameworkCore;
using PatchNotes.Data;
using Stripe;

namespace PatchNotes.Api.Webhooks;

public static class StripeWebhook
{
    public static WebApplication MapStripeWebhook(this WebApplication app)
    {
        // POST /webhooks/stripe - Handle Stripe webhook events
        app.MapPost("/webhooks/stripe", async (HttpContext httpContext, PatchNotesDbContext db, IConfiguration configuration) =>
        {
            var webhookSecret = configuration["Stripe:WebhookSecret"];

            // Read the raw body for signature verification
            using var reader = new StreamReader(httpContext.Request.Body);
            var body = await reader.ReadToEndAsync();

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    body,
                    httpContext.Request.Headers["Stripe-Signature"],
                    webhookSecret
                );
            }
            catch (StripeException ex)
            {
                Console.WriteLine($"Stripe webhook signature verification failed: {ex.Message}");
                return Results.BadRequest(new { error = "Invalid signature" });
            }

            // Filter events to only those for our app
            if (stripeEvent.Data.Object is IHasMetadata objWithMetadata)
            {
                var metadata = objWithMetadata.Metadata;
                if (metadata == null || !metadata.TryGetValue("app", out var appValue) || appValue != "patchnotes")
                {
                    // Not our event, ignore but acknowledge
                    return Results.Ok(new { received = true, ignored = true });
                }
            }

            try
            {
                switch (stripeEvent.Type)
                {
                    case "checkout.session.completed":
                        await HandleCheckoutSessionCompleted(stripeEvent, db);
                        break;

                    case "customer.subscription.updated":
                        await HandleSubscriptionUpdated(stripeEvent, db);
                        break;

                    case "customer.subscription.deleted":
                        await HandleSubscriptionDeleted(stripeEvent, db);
                        break;

                    case "invoice.payment_failed":
                        await HandlePaymentFailed(stripeEvent, db);
                        break;

                    default:
                        Console.WriteLine($"Unhandled Stripe event type: {stripeEvent.Type}");
                        break;
                }

                return Results.Ok(new { received = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling Stripe webhook: {ex.Message}");
                return Results.Problem("Error processing webhook");
            }
        });

        return app;
    }

    private static async Task HandleCheckoutSessionCompleted(Event stripeEvent, PatchNotesDbContext db)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session == null) return;

        // Get the Stytch user ID from session metadata
        if (!session.Metadata.TryGetValue("stytch_user_id", out var stytchUserId))
        {
            Console.WriteLine("Checkout session completed but no stytch_user_id in metadata");
            return;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.StytchUserId == stytchUserId);
        if (user == null)
        {
            Console.WriteLine($"User not found for Stytch ID: {stytchUserId}");
            return;
        }

        // Update user with Stripe customer ID
        user.StripeCustomerId = session.CustomerId;

        // Fetch the subscription to get status and period end
        if (!string.IsNullOrEmpty(session.SubscriptionId))
        {
            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(session.SubscriptionId);

            user.StripeSubscriptionId = subscription.Id;
            user.SubscriptionStatus = subscription.Status;
            user.SubscriptionExpiresAt = subscription.CurrentPeriodEnd;
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"Updated subscription for user {stytchUserId}: status={user.SubscriptionStatus}");
    }

    private static async Task HandleSubscriptionUpdated(Event stripeEvent, PatchNotesDbContext db)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        var user = await db.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == subscription.CustomerId);
        if (user == null)
        {
            Console.WriteLine($"User not found for Stripe customer: {subscription.CustomerId}");
            return;
        }

        user.StripeSubscriptionId = subscription.Id;
        user.SubscriptionStatus = subscription.Status;
        user.SubscriptionExpiresAt = subscription.CurrentPeriodEnd;

        await db.SaveChangesAsync();
        Console.WriteLine($"Updated subscription for customer {subscription.CustomerId}: status={subscription.Status}");
    }

    private static async Task HandleSubscriptionDeleted(Event stripeEvent, PatchNotesDbContext db)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        var user = await db.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == subscription.CustomerId);
        if (user == null)
        {
            Console.WriteLine($"User not found for Stripe customer: {subscription.CustomerId}");
            return;
        }

        user.SubscriptionStatus = "canceled";
        // Keep the expiration date so user has access until end of paid period
        user.SubscriptionExpiresAt = subscription.CurrentPeriodEnd;

        await db.SaveChangesAsync();
        Console.WriteLine($"Subscription canceled for customer {subscription.CustomerId}");
    }

    private static async Task HandlePaymentFailed(Event stripeEvent, PatchNotesDbContext db)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        var user = await db.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == invoice.CustomerId);
        if (user == null)
        {
            Console.WriteLine($"User not found for Stripe customer: {invoice.CustomerId}");
            return;
        }

        user.SubscriptionStatus = "past_due";

        await db.SaveChangesAsync();
        Console.WriteLine($"Payment failed for customer {invoice.CustomerId}, marked as past_due");
    }
}
