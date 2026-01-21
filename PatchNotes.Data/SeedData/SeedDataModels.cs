using System.Text.Json.Serialization;

namespace PatchNotes.Data.SeedData;

public class SeedPackage
{
    [JsonPropertyName("npmName")]
    public required string NpmName { get; set; }

    [JsonPropertyName("githubOwner")]
    public required string GithubOwner { get; set; }

    [JsonPropertyName("githubRepo")]
    public required string GithubRepo { get; set; }

    [JsonPropertyName("releases")]
    public List<SeedRelease> Releases { get; set; } = [];
}

public class SeedRelease
{
    [JsonPropertyName("tag")]
    public required string Tag { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }

    [JsonPropertyName("body")]
    public required string Body { get; set; }

    [JsonPropertyName("publishedAt")]
    public DateTime PublishedAt { get; set; }
}
