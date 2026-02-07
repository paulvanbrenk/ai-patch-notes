namespace PatchNotes.Data;

public class User
{
    public string Id { get; set; } = IdGenerator.NewId();

    /// <summary>
    /// The Stytch user ID (e.g., "user-live-xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx")
    /// </summary>
    public required string StytchUserId { get; set; }

    /// <summary>
    /// Primary email address from Stytch
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// User's name if provided via OAuth or profile
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// When the user first authenticated (created in our system)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the user record was last updated from Stytch
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Last time the user logged in
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Stripe customer ID for subscription management
    /// </summary>
    public string? StripeCustomerId { get; set; }

    /// <summary>
    /// Stripe subscription ID for the Pro plan
    /// </summary>
    public string? StripeSubscriptionId { get; set; }

    /// <summary>
    /// Current subscription status: "active", "canceled", "past_due", or null
    /// </summary>
    public string? SubscriptionStatus { get; set; }

    /// <summary>
    /// When the current subscription period expires
    /// </summary>
    public DateTime? SubscriptionExpiresAt { get; set; }

    /// <summary>
    /// Whether the user has an active Pro subscription
    /// </summary>
    public bool IsPro =>
        SubscriptionStatus == "active" ||
        SubscriptionStatus == "trialing" ||
        SubscriptionStatus == "past_due" ||
        (SubscriptionStatus == "canceled" && SubscriptionExpiresAt > DateTime.UtcNow);

    public ICollection<Watchlist> Watchlists { get; set; } = [];
}
