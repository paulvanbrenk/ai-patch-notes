namespace PatchNotes.Data.Stytch;

/// <summary>
/// Options for configuring the Stytch client.
/// </summary>
public class StytchClientOptions
{
    /// <summary>
    /// The section name in configuration.
    /// </summary>
    public const string SectionName = "Stytch";

    /// <summary>
    /// The Stytch API base URL. Defaults to test environment.
    /// Use https://api.stytch.com for production.
    /// </summary>
    public string BaseUrl { get; set; } = "https://test.stytch.com";

    /// <summary>
    /// The Stytch project ID (used as username for Basic auth).
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// The Stytch secret key (used as password for Basic auth).
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// The webhook signing secret for verifying webhook payloads.
    /// </summary>
    public string? WebhookSecret { get; set; }
}
