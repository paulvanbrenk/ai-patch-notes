using System.Net;
using Microsoft.Extensions.Logging;

namespace PatchNotes.Sync.GitHub;

/// <summary>
/// DelegatingHandler that enforces GitHub API rate limits by proactively
/// waiting when remaining requests are exhausted.
/// </summary>
public class RateLimitHandler : DelegatingHandler
{
    private readonly ILogger<RateLimitHandler> _logger;
    private readonly TimeProvider _timeProvider;

    // Shared state: last known rate limit info
    private int _remaining = int.MaxValue;
    private DateTimeOffset _resetAt = DateTimeOffset.MinValue;
    private readonly object _lock = new();

    /// <summary>
    /// Minimum remaining requests before proactive waiting kicks in.
    /// </summary>
    internal const int MinRemainingThreshold = 5;

    /// <summary>
    /// Maximum time we'll wait for a rate limit reset before giving up.
    /// </summary>
    internal static readonly TimeSpan MaxWaitTime = TimeSpan.FromMinutes(15);

    public RateLimitHandler(ILogger<RateLimitHandler> logger, TimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Proactive wait: if we know remaining is exhausted, wait before sending
        await WaitIfRateLimitedAsync(cancellationToken);

        var response = await base.SendAsync(request, cancellationToken);

        // Update tracked rate limit state from response headers
        UpdateRateLimitState(response);

        return response;
    }

    private async Task WaitIfRateLimitedAsync(CancellationToken cancellationToken)
    {
        int remaining;
        DateTimeOffset resetAt;

        lock (_lock)
        {
            remaining = _remaining;
            resetAt = _resetAt;
        }

        if (remaining > MinRemainingThreshold)
            return;

        var now = _timeProvider.GetUtcNow();
        var waitTime = resetAt - now;

        if (waitTime <= TimeSpan.Zero)
            return;

        if (waitTime > MaxWaitTime)
        {
            _logger.LogWarning(
                "GitHub API rate limit wait time ({WaitSeconds:F0}s) exceeds maximum ({MaxSeconds:F0}s). Proceeding without waiting",
                waitTime.TotalSeconds, MaxWaitTime.TotalSeconds);
            return;
        }

        _logger.LogWarning(
            "GitHub API rate limit nearly exhausted ({Remaining} remaining). Waiting {WaitSeconds:F1}s for reset at {ResetAt:u}",
            remaining, waitTime.TotalSeconds, resetAt);

        await Task.Delay(waitTime, _timeProvider, cancellationToken);
    }

    private void UpdateRateLimitState(HttpResponseMessage response)
    {
        var rateLimitInfo = RateLimitHelper.ParseHeaders(response.Headers);

        if (!rateLimitInfo.IsValid)
            return;

        lock (_lock)
        {
            _remaining = rateLimitInfo.Remaining;
            _resetAt = rateLimitInfo.ResetAt;
        }
    }
}
