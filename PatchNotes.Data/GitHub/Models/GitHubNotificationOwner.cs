using System.Text.Json.Serialization;

namespace PatchNotes.Data.GitHub.Models;

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
