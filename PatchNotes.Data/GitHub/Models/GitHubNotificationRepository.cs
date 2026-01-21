using System.Text.Json.Serialization;

namespace PatchNotes.Data.GitHub.Models;

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
