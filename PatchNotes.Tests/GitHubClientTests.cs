using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PatchNotes.Sync.Core.GitHub;
using PatchNotes.Sync.Core.GitHub.Models;

namespace PatchNotes.Tests;

public class GitHubClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<GitHubClient>> _mockLogger;
    private readonly GitHubClient _client;

    public GitHubClientTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHandler)
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
        _mockLogger = new Mock<ILogger<GitHubClient>>();
        _client = new GitHubClient(_httpClient, _mockLogger.Object);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetReleasesAsync Tests

    [Fact]
    public async Task GetReleasesAsync_GivenValidRepo_ReturnsReleases()
    {
        // Arrange
        var releases = new[]
        {
            new { id = 1, tag_name = "v1.0.0", name = "Release 1", body = "Body 1", draft = false, prerelease = false, published_at = DateTimeOffset.UtcNow },
            new { id = 2, tag_name = "v1.1.0", name = "Release 2", body = "Body 2", draft = false, prerelease = true, published_at = DateTimeOffset.UtcNow }
        };
        _mockHandler.SetupResponse("repos/owner/repo/releases?per_page=30&page=1", releases);

        // Act
        var result = await _client.GetReleasesAsync("owner", "repo");

        // Assert
        result.Should().HaveCount(2);
        result[0].TagName.Should().Be("v1.0.0");
        result[1].TagName.Should().Be("v1.1.0");
        result[1].Prerelease.Should().BeTrue();
    }

    [Fact]
    public async Task GetReleasesAsync_WithPagination_UsesCorrectParameters()
    {
        // Arrange
        _mockHandler.SetupResponse("repos/owner/repo/releases?per_page=50&page=3", Array.Empty<object>());

        // Act
        await _client.GetReleasesAsync("owner", "repo", perPage: 50, page: 3);

        // Assert
        _mockHandler.LastRequestUri.Should().Contain("per_page=50");
        _mockHandler.LastRequestUri.Should().Contain("page=3");
    }

    [Fact]
    public async Task GetReleasesAsync_WithInvalidOwner_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _client.GetReleasesAsync("", "repo"));
    }

    [Fact]
    public async Task GetReleasesAsync_WithInvalidPerPage_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _client.GetReleasesAsync("owner", "repo", perPage: 101));
    }

    [Fact]
    public async Task GetReleasesAsync_GivenOwnerOrRepoWithSpecialChars_EscapesThemInUrl()
    {
        // Arrange
        _mockHandler.SetupResponse("repos/owner%2Fspecial/repo%2Fname/releases?per_page=30&page=1", Array.Empty<object>());

        // Act
        await _client.GetReleasesAsync("owner/special", "repo/name");

        // Assert
        _mockHandler.LastRequestUri.Should().Contain("owner%2Fspecial");
        _mockHandler.LastRequestUri.Should().Contain("repo%2Fname");
    }

    #endregion

    #region GetAllReleasesAsync Tests

    [Fact]
    public async Task GetAllReleasesAsync_WithSinglePage_ReturnsAllReleases()
    {
        // Arrange
        var releases = Enumerable.Range(1, 10)
            .Select(i => new { id = (long)i, tag_name = $"v{i}.0.0", draft = false, published_at = DateTimeOffset.UtcNow })
            .ToArray();
        _mockHandler.SetupResponse("repos/owner/repo/releases?per_page=100&page=1", releases);

        // Act
        var result = new List<GitHubRelease>();
        await foreach (var release in _client.GetAllReleasesAsync("owner", "repo"))
        {
            result.Add(release);
        }

        // Assert
        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetAllReleasesAsync_WithMultiplePages_FetchesAllPages()
    {
        // Arrange
        var page1 = Enumerable.Range(1, 100)
            .Select(i => new { id = (long)i, tag_name = $"v{i}.0.0", draft = false, published_at = DateTimeOffset.UtcNow })
            .ToArray();
        var page2 = Enumerable.Range(101, 50)
            .Select(i => new { id = (long)i, tag_name = $"v{i}.0.0", draft = false, published_at = DateTimeOffset.UtcNow })
            .ToArray();

        _mockHandler.SetupResponse("repos/owner/repo/releases?per_page=100&page=1", page1);
        _mockHandler.SetupResponse("repos/owner/repo/releases?per_page=100&page=2", page2);

        // Act
        var result = new List<GitHubRelease>();
        await foreach (var release in _client.GetAllReleasesAsync("owner", "repo"))
        {
            result.Add(release);
        }

        // Assert
        result.Should().HaveCount(150);
        _mockHandler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllReleasesAsync_GivenEmptyPage_StopsPagination()
    {
        // Arrange
        var page1 = Enumerable.Range(1, 100)
            .Select(i => new { id = (long)i, tag_name = $"v{i}.0.0", draft = false, published_at = DateTimeOffset.UtcNow })
            .ToArray();
        _mockHandler.SetupResponse("repos/owner/repo/releases?per_page=100&page=1", page1);
        _mockHandler.SetupResponse("repos/owner/repo/releases?per_page=100&page=2", Array.Empty<object>());

        // Act
        var result = new List<GitHubRelease>();
        await foreach (var release in _client.GetAllReleasesAsync("owner", "repo"))
        {
            result.Add(release);
        }

        // Assert
        result.Should().HaveCount(100);
        _mockHandler.RequestCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAllReleasesAsync_WithCancellation_StopsIteration()
    {
        // Arrange
        var page1 = Enumerable.Range(1, 100)
            .Select(i => new { id = (long)i, tag_name = $"v{i}.0.0", draft = false, published_at = DateTimeOffset.UtcNow })
            .ToArray();
        _mockHandler.SetupResponse("repos/owner/repo/releases?per_page=100&page=1", page1);
        _mockHandler.SetupResponse("repos/owner/repo/releases?per_page=100&page=2", page1);

        using var cts = new CancellationTokenSource();
        var count = 0;

        // Act
        await foreach (var release in _client.GetAllReleasesAsync("owner", "repo", cts.Token))
        {
            count++;
            if (count == 50)
            {
                cts.Cancel();
                break;
            }
        }

        // Assert
        count.Should().Be(50);
        _mockHandler.RequestCount.Should().Be(1);
    }

    #endregion

    #region Rate Limit Tests

    [Fact]
    public async Task GetReleasesAsync_GivenRateLimitHeaders_ParsesThemCorrectly()
    {
        // Arrange
        var resetTime = DateTimeOffset.UtcNow.AddMinutes(30);
        _mockHandler.SetupResponseWithRateLimits(
            "repos/owner/repo/releases?per_page=30&page=1",
            Array.Empty<object>(),
            limit: 5000,
            remaining: 4999,
            used: 1,
            reset: resetTime.ToUnixTimeSeconds());

        // Act
        await _client.GetReleasesAsync("owner", "repo");

        // Assert - verify logging was called (indicates headers were parsed)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("rate limit")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetReleasesAsync_GivenRateLimitNearlyExhausted_LogsWarning()
    {
        // Arrange
        var resetTime = DateTimeOffset.UtcNow.AddMinutes(30);
        _mockHandler.SetupResponseWithRateLimits(
            "repos/owner/repo/releases?per_page=30&page=1",
            Array.Empty<object>(),
            limit: 5000,
            remaining: 5,  // Less than 10%, should trigger warning
            used: 4995,
            reset: resetTime.ToUnixTimeSeconds());

        // Act
        await _client.GetReleasesAsync("owner", "repo");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("approaching")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region SearchRepositoriesAsync Tests

    [Fact]
    public async Task SearchRepositoriesAsync_GivenValidQuery_ReturnsResults()
    {
        // Arrange
        var searchResponse = new
        {
            total_count = 2,
            items = new[]
            {
                new { full_name = "facebook/react", owner = new { login = "facebook" }, name = "react", description = "A JavaScript library for building user interfaces", stargazers_count = 200000 },
                new { full_name = "facebook/react-native", owner = new { login = "facebook" }, name = "react-native", description = "React Native", stargazers_count = 100000 }
            }
        };
        _mockHandler.SetupResponse("search/repositories?q=react&per_page=10", searchResponse);

        // Act
        var result = await _client.SearchRepositoriesAsync("react");

        // Assert
        result.Should().HaveCount(2);
        result[0].Owner.Login.Should().Be("facebook");
        result[0].Name.Should().Be("react");
        result[0].Description.Should().Be("A JavaScript library for building user interfaces");
        result[0].StargazersCount.Should().Be(200000);
    }

    [Fact]
    public async Task SearchRepositoriesAsync_WithCustomPerPage_UsesCorrectParameter()
    {
        // Arrange
        var searchResponse = new { total_count = 0, items = Array.Empty<object>() };
        _mockHandler.SetupResponse("search/repositories?q=test&per_page=5", searchResponse);

        // Act
        await _client.SearchRepositoriesAsync("test", perPage: 5);

        // Assert
        _mockHandler.LastRequestUri.Should().Contain("per_page=5");
    }

    [Fact]
    public async Task SearchRepositoriesAsync_WithEmptyQuery_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _client.SearchRepositoriesAsync(""));
    }

    [Fact]
    public async Task SearchRepositoriesAsync_WithInvalidPerPage_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _client.SearchRepositoriesAsync("react", perPage: 101));
    }

    [Fact]
    public async Task SearchRepositoriesAsync_GivenQueryWithSpecialChars_EscapesQueryString()
    {
        // Arrange
        var searchResponse = new { total_count = 0, items = Array.Empty<object>() };
        _mockHandler.SetupResponse("search/repositories?q=c%23%20library&per_page=10", searchResponse);

        // Act
        await _client.SearchRepositoriesAsync("c# library");

        // Assert
        _mockHandler.LastRequestUri.Should().Contain("q=c%23%20library");
    }

    [Fact]
    public async Task SearchRepositoriesAsync_WithNoResults_ReturnsEmptyList()
    {
        // Arrange
        var searchResponse = new { total_count = 0, items = Array.Empty<object>() };
        _mockHandler.SetupResponse("search/repositories?q=xyznonexistent&per_page=10", searchResponse);

        // Act
        var result = await _client.SearchRepositoriesAsync("xyznonexistent");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetReleasesAsync_WithHttpError_ThrowsHttpRequestException()
    {
        // Arrange
        _mockHandler.SetupErrorResponse("repos/owner/repo/releases?per_page=30&page=1", HttpStatusCode.NotFound);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _client.GetReleasesAsync("owner", "repo"));
    }

    [Fact]
    public async Task GetReleasesAsync_WithRateLimitExceeded_ThrowsHttpRequestException()
    {
        // Arrange
        _mockHandler.SetupErrorResponse("repos/owner/repo/releases?per_page=30&page=1", HttpStatusCode.Forbidden);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _client.GetReleasesAsync("owner", "repo"));
    }

    #endregion
}

/// <summary>
/// Mock HTTP message handler for testing HTTP client behavior.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, HttpResponseMessage> _responses = new();
    public string? LastRequestUri { get; private set; }
    public int RequestCount { get; private set; }

    public void SetupResponse(string pathAndQuery, object content)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(content)
        };
        _responses[pathAndQuery] = response;
    }

    public void SetupResponseWithRateLimits(
        string pathAndQuery,
        object content,
        int limit,
        int remaining,
        int used,
        long reset)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(content)
        };
        response.Headers.Add("X-RateLimit-Limit", limit.ToString());
        response.Headers.Add("X-RateLimit-Remaining", remaining.ToString());
        response.Headers.Add("X-RateLimit-Used", used.ToString());
        response.Headers.Add("X-RateLimit-Reset", reset.ToString());
        _responses[pathAndQuery] = response;
    }

    public void SetupErrorResponse(string pathAndQuery, HttpStatusCode statusCode)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("")
        };
        _responses[pathAndQuery] = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestCount++;
        LastRequestUri = request.RequestUri?.PathAndQuery;

        var pathAndQuery = request.RequestUri?.PathAndQuery.TrimStart('/') ?? "";

        if (_responses.TryGetValue(pathAndQuery, out var response))
        {
            return Task.FromResult(response);
        }

        // Fail loudly for unmatched requests so tests don't silently pass
        // with empty data. Every expected HTTP call must be explicitly set up.
        throw new InvalidOperationException(
            $"No mock response configured for {request.Method} {pathAndQuery}. " +
            $"Call SetupResponse/SetupErrorResponse for this path in your test arrangement.");
    }
}
