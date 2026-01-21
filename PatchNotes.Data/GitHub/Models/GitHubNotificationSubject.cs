using System.Text.Json.Serialization;

namespace PatchNotes.Data.GitHub.Models;

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
