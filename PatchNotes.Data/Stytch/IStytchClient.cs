namespace PatchNotes.Data.Stytch;

/// <summary>
/// Interface for Stytch API operations.
/// </summary>
public interface IStytchClient
{
    /// <summary>
    /// Authenticates a session token and returns the user ID if valid.
    /// </summary>
    /// <param name="sessionToken">The session token from the cookie.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session result with user info, or null if invalid.</returns>
    Task<StytchSessionResult?> AuthenticateSessionAsync(string sessionToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by their Stytch user ID.
    /// </summary>
    /// <param name="userId">The Stytch user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user info, or null if not found.</returns>
    Task<StytchUser?> GetUserAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a successful session authentication.
/// </summary>
public class StytchSessionResult
{
    /// <summary>
    /// The Stytch user ID.
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// The session ID.
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// The user's primary email, if available.
    /// </summary>
    public string? Email { get; set; }
}

/// <summary>
/// Stytch user information.
/// </summary>
public class StytchUser
{
    /// <summary>
    /// The Stytch user ID.
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// The user's primary email, if available.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// The user's name, if available.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The user's status (active, pending, deleted).
    /// </summary>
    public string? Status { get; set; }
}
