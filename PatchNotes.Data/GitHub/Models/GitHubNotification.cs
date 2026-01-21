using System.Text.Json.Serialization;

namespace PatchNotes.Data.GitHub.Models;

/// <summary>
/// Represents a notification from the GitHub API.
/// </summary>
public class GitHubNotification
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("unread")]
    public bool Unread { get; set; }

    [JsonPropertyName("reason")]
    public required string Reason { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("last_read_at")]
    public DateTime? LastReadAt { get; set; }

    [JsonPropertyName("subject")]
    public required GitHubNotificationSubject Subject { get; set; }

    [JsonPropertyName("repository")]
    public required GitHubNotificationRepository Repository { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("subscription_url")]
    public string? SubscriptionUrl { get; set; }
}
