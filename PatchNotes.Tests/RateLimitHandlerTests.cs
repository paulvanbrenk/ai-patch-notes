using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PatchNotes.Sync.GitHub;

namespace PatchNotes.Tests;

public class RateLimitHandlerTests : IDisposable
{
    private readonly Mock<ILogger<RateLimitHandler>> _mockLogger;
    private readonly FakeTimeProvider _timeProvider;
    private readonly RateLimitHandler _handler;
    private readonly InnerHandler _innerHandler;
    private readonly HttpClient _httpClient;

    public RateLimitHandlerTests()
    {
        _mockLogger = new Mock<ILogger<RateLimitHandler>>();
        _timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _innerHandler = new InnerHandler();
        _handler = new RateLimitHandler(_mockLogger.Object, _timeProvider)
        {
            InnerHandler = _innerHandler
        };
        _httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SendAsync_WithPlentyOfRemainingRequests_DoesNotDelay()
    {
        // Arrange: response indicates plenty of remaining requests
        _innerHandler.SetupResponse(rateLimitRemaining: 4000, rateLimitLimit: 5000,
            resetAt: _timeProvider.GetUtcNow().AddMinutes(30));

        // Act
        var response = await _httpClient.GetAsync("repos/owner/repo/releases");

        // Assert: should complete without delay
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _innerHandler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_TracksRateLimitStateFromResponses()
    {
        // Arrange: first request returns low remaining
        _innerHandler.SetupResponse(rateLimitRemaining: 3, rateLimitLimit: 5000,
            resetAt: _timeProvider.GetUtcNow().AddSeconds(2));

        // Act: first request - should go through (no prior state)
        await _httpClient.GetAsync("repos/owner/repo/releases");

        // Advance time past reset so we don't actually wait
        _timeProvider.Advance(TimeSpan.FromSeconds(3));

        // Second request: handler knows remaining is low but reset has passed
        _innerHandler.SetupResponse(rateLimitRemaining: 5000, rateLimitLimit: 5000,
            resetAt: _timeProvider.GetUtcNow().AddMinutes(60));
        await _httpClient.GetAsync("repos/owner/repo/releases");

        // Assert: both requests went through
        _innerHandler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_WhenRemainingExhausted_WaitsForReset()
    {
        // Arrange: first response sets remaining to 0
        var resetTime = _timeProvider.GetUtcNow().AddSeconds(5);
        _innerHandler.SetupResponse(rateLimitRemaining: 0, rateLimitLimit: 5000, resetAt: resetTime);

        // First request goes through (no prior state)
        await _httpClient.GetAsync("repos/owner/repo/releases");

        // Set up response for second request
        _innerHandler.SetupResponse(rateLimitRemaining: 5000, rateLimitLimit: 5000,
            resetAt: _timeProvider.GetUtcNow().AddMinutes(60));

        // Act: second request - should trigger wait since remaining is 0
        var requestTask = _httpClient.GetAsync("repos/owner/repo/releases");

        // Advance time past reset
        _timeProvider.Advance(TimeSpan.FromSeconds(6));

        var response = await requestTask;

        // Assert: second request eventually completed
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _innerHandler.RequestCount.Should().Be(2);

        // Should have logged a warning about waiting
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("exhausted") || v.ToString()!.Contains("Waiting")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SendAsync_WhenResetAlreadyPassed_DoesNotDelay()
    {
        // Arrange: first response sets remaining to 0 but reset is in the past
        var resetTime = _timeProvider.GetUtcNow().AddSeconds(-10);
        _innerHandler.SetupResponse(rateLimitRemaining: 0, rateLimitLimit: 5000, resetAt: resetTime);

        // First request
        await _httpClient.GetAsync("repos/owner/repo/releases");

        // Set up response for second request
        _innerHandler.SetupResponse(rateLimitRemaining: 5000, rateLimitLimit: 5000,
            resetAt: _timeProvider.GetUtcNow().AddMinutes(60));

        // Act: second request - reset is in the past, should not wait
        var response = await _httpClient.GetAsync("repos/owner/repo/releases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _innerHandler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_WhenWaitExceedsMaximum_ProceedsWithoutWaiting()
    {
        // Arrange: remaining exhausted with reset far in the future
        var resetTime = _timeProvider.GetUtcNow().AddMinutes(60);
        _innerHandler.SetupResponse(rateLimitRemaining: 0, rateLimitLimit: 5000, resetAt: resetTime);

        // First request to prime state
        await _httpClient.GetAsync("repos/owner/repo/releases");

        // Set up response for second request
        _innerHandler.SetupResponse(rateLimitRemaining: 5000, rateLimitLimit: 5000,
            resetAt: _timeProvider.GetUtcNow().AddMinutes(120));

        // Act: should proceed without waiting (60 min > MaxWaitTime of 15 min)
        var response = await _httpClient.GetAsync("repos/owner/repo/releases");

        // Assert: request went through despite exhausted limit
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _innerHandler.RequestCount.Should().Be(2);

        // Should log warning about exceeding max wait
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("exceeds maximum")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithNoRateLimitHeaders_DoesNotTrackState()
    {
        // Arrange: response without rate limit headers
        _innerHandler.SetupResponse(rateLimitRemaining: null, rateLimitLimit: null, resetAt: null);

        // Act
        var response = await _httpClient.GetAsync("repos/owner/repo/releases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _innerHandler.RequestCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_WithRemainingAboveThreshold_DoesNotDelay()
    {
        // Arrange: remaining is above the threshold (5) but below 10%
        var resetTime = _timeProvider.GetUtcNow().AddMinutes(30);
        _innerHandler.SetupResponse(rateLimitRemaining: 10, rateLimitLimit: 5000, resetAt: resetTime);

        // First request to set state
        await _httpClient.GetAsync("repos/owner/repo/releases");

        // Second request should not wait since 10 > MinRemainingThreshold (5)
        _innerHandler.SetupResponse(rateLimitRemaining: 5000, rateLimitLimit: 5000,
            resetAt: _timeProvider.GetUtcNow().AddMinutes(60));

        var response = await _httpClient.GetAsync("repos/owner/repo/releases");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _innerHandler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task SendAsync_RespectsCancellationToken()
    {
        // Arrange
        var resetTime = _timeProvider.GetUtcNow().AddSeconds(30);
        _innerHandler.SetupResponse(rateLimitRemaining: 0, rateLimitLimit: 5000, resetAt: resetTime);

        // First request to prime state
        await _httpClient.GetAsync("repos/owner/repo/releases");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert: should throw OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _httpClient.GetAsync("repos/owner/repo/releases", cts.Token));
    }

    /// <summary>
    /// Inner handler that returns configurable responses with rate limit headers.
    /// </summary>
    private class InnerHandler : HttpMessageHandler
    {
        private int? _rateLimitRemaining;
        private int? _rateLimitLimit;
        private DateTimeOffset? _resetAt;

        public int RequestCount { get; private set; }

        public void SetupResponse(int? rateLimitRemaining, int? rateLimitLimit, DateTimeOffset? resetAt)
        {
            _rateLimitRemaining = rateLimitRemaining;
            _rateLimitLimit = rateLimitLimit;
            _resetAt = resetAt;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(Array.Empty<object>())
            };

            if (_rateLimitLimit.HasValue)
                response.Headers.Add("X-RateLimit-Limit", _rateLimitLimit.Value.ToString());
            if (_rateLimitRemaining.HasValue)
                response.Headers.Add("X-RateLimit-Remaining", _rateLimitRemaining.Value.ToString());
            if (_resetAt.HasValue)
                response.Headers.Add("X-RateLimit-Reset", _resetAt.Value.ToUnixTimeSeconds().ToString());

            return Task.FromResult(response);
        }
    }
}

/// <summary>
/// Fake TimeProvider for testing time-dependent behavior.
/// </summary>
public class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FakeTimeProvider(DateTimeOffset startTime)
    {
        _utcNow = startTime;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan duration)
    {
        _utcNow += duration;
    }
}
