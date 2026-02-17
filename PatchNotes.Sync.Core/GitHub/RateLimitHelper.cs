using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using PatchNotes.Sync.Core.GitHub.Models;

namespace PatchNotes.Sync.Core.GitHub;

/// <summary>
/// Helper class for parsing and logging GitHub API rate limit information.
/// </summary>
public static class RateLimitHelper
{
    /// <summary>
    /// Parses rate limit information from GitHub API response headers.
    /// </summary>
    public static GitHubRateLimitInfo ParseHeaders(HttpResponseHeaders headers)
    {
        int.TryParse(GetHeaderValue(headers, "X-RateLimit-Limit"), out var limit);
        int.TryParse(GetHeaderValue(headers, "X-RateLimit-Remaining"), out var remaining);
        int.TryParse(GetHeaderValue(headers, "X-RateLimit-Used"), out var used);
        long.TryParse(GetHeaderValue(headers, "X-RateLimit-Reset"), out var resetUnix);

        var resetAt = resetUnix > 0
            ? DateTimeOffset.FromUnixTimeSeconds(resetUnix)
            : DateTimeOffset.MinValue;

        return new GitHubRateLimitInfo
        {
            Limit = limit,
            Remaining = remaining,
            Used = used,
            ResetAt = resetAt
        };
    }

    /// <summary>
    /// Logs rate limit status with appropriate log level based on remaining requests.
    /// </summary>
    public static void LogStatus(ILogger logger, GitHubRateLimitInfo rateLimitInfo, string? context = null)
    {
        if (!rateLimitInfo.IsValid)
        {
            return;
        }

        var contextSuffix = string.IsNullOrEmpty(context) ? "" : $" for {context}";

        if (rateLimitInfo.IsApproachingLimit(10))
        {
            logger.LogWarning(
                "GitHub API rate limit approaching{Context}: {Remaining}/{Limit} requests remaining ({Percentage:F1}%). Resets at {ResetAt:u}",
                contextSuffix,
                rateLimitInfo.Remaining,
                rateLimitInfo.Limit,
                rateLimitInfo.RemainingPercentage,
                rateLimitInfo.ResetAt);
        }
        else
        {
            logger.LogDebug(
                "GitHub API rate limit status: {Remaining}/{Limit} requests remaining. Resets at {ResetAt:u}",
                rateLimitInfo.Remaining,
                rateLimitInfo.Limit,
                rateLimitInfo.ResetAt);
        }
    }

    private static string? GetHeaderValue(HttpResponseHeaders headers, string name) =>
        headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
}
