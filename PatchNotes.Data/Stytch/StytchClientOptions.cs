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
    /// The Stytch project ID.
    /// The SDK determines test vs live environment based on the project ID prefix.
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// The Stytch secret key.
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// The webhook signing secret for verifying webhook payloads.
    /// </summary>
    public string? WebhookSecret { get; set; }
}
