namespace PatchNotes.Data;

public class Notification
{
    public int Id { get; set; }
    public required string GitHubId { get; set; }
    public int? PackageId { get; set; }
    public required string Reason { get; set; }
    public required string SubjectTitle { get; set; }
    public required string SubjectType { get; set; }
    public string? SubjectUrl { get; set; }
    public required string RepositoryFullName { get; set; }
    public bool Unread { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastReadAt { get; set; }
    public DateTime FetchedAt { get; set; }

    public Package? Package { get; set; }
}
