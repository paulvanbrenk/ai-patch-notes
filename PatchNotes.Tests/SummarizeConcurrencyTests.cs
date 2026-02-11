using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PatchNotes.Data;
using PatchNotes.Data.AI;
using ReleaseInput = PatchNotes.Data.AI.ReleaseInput;

namespace PatchNotes.Tests;

public class SummarizeConcurrencyTests
{
    /// <summary>
    /// Verifies that the SummaryVersion concurrency token detects conflicting saves.
    /// Two DbContext instances load the same release, both modify it, and the second
    /// save should throw DbUpdateConcurrencyException.
    /// </summary>
    [Fact]
    public async Task SummaryVersion_DetectsConflictingUpdates()
    {
        // Use a shared SQLite connection so both contexts see the same data
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PatchNotesDbContext>()
            .UseSqlite(connection)
            .Options;

        // Create schema and seed data
        await using (var db = new PatchNotesDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            var package = new Package
            {
                Name = "test-pkg", Url = "https://github.com/o/r",
                NpmName = "test-pkg", GithubOwner = "o", GithubRepo = "r",
                CreatedAt = DateTime.UtcNow
            };
            db.Packages.Add(package);
            await db.SaveChangesAsync();

            db.Releases.Add(new Release
            {
                PackageId = package.Id,
                Tag = "v1.0.0",
                PublishedAt = DateTime.UtcNow,
                FetchedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Load the release in two separate contexts (simulating two concurrent requests)
        await using var ctx1 = new PatchNotesDbContext(options);
        await using var ctx2 = new PatchNotesDbContext(options);

        var release1 = await ctx1.Releases.FirstAsync();
        var release2 = await ctx2.Releases.FirstAsync();

        // First "request" saves successfully
        release1.Summary = "Summary from request 1";
        release1.SummaryGeneratedAt = DateTime.UtcNow;
        release1.SummaryStale = false;
        release1.SummaryVersion = IdGenerator.NewId();
        await ctx1.SaveChangesAsync();

        // Second "request" tries to save with stale SummaryVersion - should fail
        release2.Summary = "Summary from request 2";
        release2.SummaryGeneratedAt = DateTime.UtcNow;
        release2.SummaryStale = false;
        release2.SummaryVersion = IdGenerator.NewId();

        var act = () => ctx2.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    /// <summary>
    /// Verifies that two concurrent non-streaming summarize requests both succeed
    /// without throwing errors. The second request should gracefully handle the
    /// concurrency conflict and return a valid summary.
    /// </summary>
    [Fact]
    public async Task ConcurrentSummarizeRequests_BothSucceed()
    {
        // Create a fixture with a slow mock AI client that enables the race condition
        await using var fixture = new SummarizeTestFixture(aiDelay: TimeSpan.FromMilliseconds(200));
        await fixture.InitializeAsync();

        // Seed a release that needs a summary
        var releaseId = await fixture.SeedReleaseNeedingSummary();

        // Fire two concurrent requests
        using var client1 = fixture.CreateAuthenticatedClient();
        using var client2 = fixture.CreateAuthenticatedClient();

        var task1 = client1.PostAsync($"/api/releases/{releaseId}/summarize", null);
        var task2 = client2.PostAsync($"/api/releases/{releaseId}/summarize", null);

        var responses = await Task.WhenAll(task1, task2);

        // Both should succeed (200 OK)
        responses[0].StatusCode.Should().Be(HttpStatusCode.OK);
        responses[1].StatusCode.Should().Be(HttpStatusCode.OK);

        // Both should return a valid summary
        var result1 = await responses[0].Content.ReadFromJsonAsync<JsonElement>();
        var result2 = await responses[1].Content.ReadFromJsonAsync<JsonElement>();

        result1.GetProperty("summary").GetString().Should().NotBeNullOrEmpty();
        result2.GetProperty("summary").GetString().Should().NotBeNullOrEmpty();
    }

    /// <summary>
    /// Verifies that when the AI client throws during SSE streaming, the client
    /// receives an error event followed by [DONE] instead of a broken stream.
    /// </summary>
    [Fact]
    public async Task StreamingSummarize_AiClientThrows_SendsErrorEvent()
    {
        await using var fixture = new SummarizeTestFixture(throwingAiClient: true);
        await fixture.InitializeAsync();

        var releaseId = await fixture.SeedReleaseNeedingSummary();

        using var client = fixture.CreateAuthenticatedClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/releases/{releaseId}/summarize");
        request.Headers.Add("Accept", "text/event-stream");

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");

        var body = await response.Content.ReadAsStringAsync();

        // Should contain an error event
        body.Should().Contain("\"type\":\"error\"");
        body.Should().Contain("AI summarization service is currently unavailable");

        // Should NOT contain a complete event
        body.Should().NotContain("\"type\":\"complete\"");

        // Should end with [DONE]
        body.Should().Contain("[DONE]");
    }

    /// <summary>
    /// Verifies that SSE streaming events include sequential id: fields for resumability.
    /// </summary>
    [Fact]
    public async Task StreamingSummarize_EventsIncludeSequentialIds()
    {
        await using var fixture = new SummarizeTestFixture();
        await fixture.InitializeAsync();

        var releaseId = await fixture.SeedReleaseNeedingSummary();

        using var client = fixture.CreateAuthenticatedClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/releases/{releaseId}/summarize");
        request.Headers.Add("Accept", "text/event-stream");

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");

        var body = await response.Content.ReadAsStringAsync();
        var lines = body.Split('\n');

        // Extract all id: values in order
        var ids = lines
            .Where(l => l.StartsWith("id: "))
            .Select(l => int.Parse(l.Substring(4).Trim()))
            .ToList();

        // DelayedMockAiClient yields 3 chunks + 1 complete + 1 [DONE] = 5 events
        ids.Should().HaveCountGreaterThanOrEqualTo(3, "each SSE event should have an id");
        ids.Should().BeInAscendingOrder("event ids should be sequential");
        ids.First().Should().Be(1, "event ids should start at 1");
    }

    /// <summary>
    /// Verifies that after concurrent requests, only one summary is persisted
    /// (no corruption or duplicate writes).
    /// </summary>
    [Fact]
    public async Task ConcurrentSummarizeRequests_PersistExactlyOneSummary()
    {
        await using var fixture = new SummarizeTestFixture(aiDelay: TimeSpan.FromMilliseconds(200));
        await fixture.InitializeAsync();

        var releaseId = await fixture.SeedReleaseNeedingSummary();

        using var client1 = fixture.CreateAuthenticatedClient();
        using var client2 = fixture.CreateAuthenticatedClient();

        var task1 = client1.PostAsync($"/api/releases/{releaseId}/summarize", null);
        var task2 = client2.PostAsync($"/api/releases/{releaseId}/summarize", null);
        await Task.WhenAll(task1, task2);

        // Verify the database has exactly one summary persisted
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var release = await db.Releases.FirstAsync(r => r.Id == releaseId);

        release.Summary.Should().NotBeNullOrEmpty();
        release.SummaryGeneratedAt.Should().NotBeNull();
        release.SummaryStale.Should().BeFalse();
        release.SummaryVersion.Should().NotBeNullOrEmpty();
    }
}

/// <summary>
/// Test fixture with a controllable mock AI client for concurrency testing.
/// </summary>
internal class SummarizeTestFixture : PatchNotesApiFixture
{
    private readonly TimeSpan _aiDelay;
    private readonly bool _throwingAiClient;

    public SummarizeTestFixture(TimeSpan aiDelay = default, bool throwingAiClient = false)
    {
        _aiDelay = aiDelay;
        _throwingAiClient = throwingAiClient;
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAiClient>();
            if (_throwingAiClient)
                services.AddSingleton<IAiClient>(new ThrowingMockAiClient());
            else
                services.AddSingleton<IAiClient>(new DelayedMockAiClient(_aiDelay));
        });
    }

    public async Task<string> SeedReleaseNeedingSummary()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var package = new Package
        {
            Name = "concurrency-test-pkg",
            Url = "https://github.com/owner/repo",
            NpmName = "concurrency-test-pkg",
            GithubOwner = "owner",
            GithubRepo = "repo",
            CreatedAt = DateTime.UtcNow
        };
        db.Packages.Add(package);
        await db.SaveChangesAsync();

        var release = new Release
        {
            PackageId = package.Id,
            Tag = "v1.0.0",
            Title = "Test Release",
            Body = "Some release notes",
            PublishedAt = DateTime.UtcNow,
            FetchedAt = DateTime.UtcNow,
            // SummaryStale defaults to true, so NeedsSummary is true
        };
        db.Releases.Add(release);
        await db.SaveChangesAsync();

        return release.Id;
    }
}

/// <summary>
/// Mock AI client that introduces a delay to create a window for race conditions.
/// </summary>
internal class DelayedMockAiClient : IAiClient
{
    private readonly TimeSpan _delay;

    public DelayedMockAiClient(TimeSpan delay)
    {
        _delay = delay;
    }

    public async Task<string> SummarizeReleaseNotesAsync(string packageName, IReadOnlyList<ReleaseInput> releases, CancellationToken cancellationToken = default)
    {
        await Task.Delay(_delay, cancellationToken);
        return $"Mock summary of {packageName} ({releases.Count} releases)";
    }

    public async IAsyncEnumerable<string> SummarizeReleaseNotesStreamAsync(string packageName, IReadOnlyList<ReleaseInput> releases, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(_delay, cancellationToken);
        yield return "Mock ";
        yield return "streamed ";
        yield return "summary";
    }
}

/// <summary>
/// Mock AI client that throws an exception to simulate provider outages.
/// </summary>
internal class ThrowingMockAiClient : IAiClient
{
    public Task<string> SummarizeReleaseNotesAsync(string packageName, IReadOnlyList<ReleaseInput> releases, CancellationToken cancellationToken = default)
    {
        throw new HttpRequestException("Simulated AI provider outage");
    }

    public async IAsyncEnumerable<string> SummarizeReleaseNotesStreamAsync(string packageName, IReadOnlyList<ReleaseInput> releases, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        throw new HttpRequestException("Simulated AI provider outage");
#pragma warning disable CS0162 // unreachable but required for IAsyncEnumerable
        yield break;
#pragma warning restore CS0162
    }
}
