using System.Text.Json.Serialization;

namespace PatchNotes.Data.GitHub;

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

/// <summary>
/// The subject of a GitHub notification.
/// </summary>
public class GitHubNotificationSubject
{
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("latest_comment_url")]
    public string? LatestCommentUrl { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }
}

/// <summary>
/// Repository information in a GitHub notification.
/// </summary>
public class GitHubNotificationRepository
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("full_name")]
    public required string FullName { get; set; }

    [JsonPropertyName("owner")]
    public required GitHubNotificationOwner Owner { get; set; }
}

/// <summary>
/// Owner information in a GitHub notification repository.
/// </summary>
public class GitHubNotificationOwner
{
    [JsonPropertyName("login")]
    public required string Login { get; set; }

    [JsonPropertyName("id")]
    public long Id { get; set; }
}
