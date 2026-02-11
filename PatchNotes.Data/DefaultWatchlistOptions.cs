namespace PatchNotes.Data;

public class DefaultWatchlistOptions
{
    public const string SectionName = "DefaultWatchlist";

    /// <summary>
    /// List of GitHub full names (e.g., "dotnet/runtime") to auto-populate for new users.
    /// </summary>
    public string[] Packages { get; set; } = [];
}
