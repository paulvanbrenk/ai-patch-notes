namespace PatchNotes.Data;

public class User
{
    public int Id { get; set; }

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
}
