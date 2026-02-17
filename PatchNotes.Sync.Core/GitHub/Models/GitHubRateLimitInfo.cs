namespace PatchNotes.Sync.Core.GitHub.Models;

/// <summary>
/// Contains rate limit information from GitHub API responses.
/// </summary>
public class GitHubRateLimitInfo
{
    /// <summary>
    /// The maximum number of requests permitted per hour.
    /// </summary>
    public int Limit { get; init; }

    /// <summary>
    /// The number of requests remaining in the current rate limit window.
    /// </summary>
    public int Remaining { get; init; }

    /// <summary>
    /// The time at which the current rate limit window resets.
    /// </summary>
    public DateTimeOffset ResetAt { get; init; }

    /// <summary>
    /// The number of requests made in the current rate limit window.
    /// </summary>
    public int Used { get; init; }

    /// <summary>
    /// Returns true if rate limit information was successfully parsed from headers.
    /// </summary>
    public bool IsValid => Limit > 0;

    /// <summary>
    /// Returns the percentage of rate limit remaining (0-100).
    /// </summary>
    public double RemainingPercentage => Limit > 0 ? (double)Remaining / Limit * 100 : 100;

    /// <summary>
    /// Returns true if the remaining requests are below the threshold percentage.
    /// </summary>
    public bool IsApproachingLimit(int thresholdPercentage = 10) =>
        IsValid && RemainingPercentage <= thresholdPercentage;
}
