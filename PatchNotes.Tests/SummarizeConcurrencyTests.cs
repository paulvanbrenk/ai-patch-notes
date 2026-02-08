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
        release1.SummaryVersion = Guid.NewGuid();
        await ctx1.SaveChangesAsync();

        // Second "request" tries to save with stale SummaryVersion - should fail
        release2.Summary = "Summary from request 2";
        release2.SummaryGeneratedAt = DateTime.UtcNow;
        release2.SummaryStale = false;
        release2.SummaryVersion = Guid.NewGuid();

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
        release.SummaryVersion.Should().NotBe(Guid.Empty);
    }
}

/// <summary>
/// Test fixture with a controllable mock AI client for concurrency testing.
/// </summary>
internal class SummarizeTestFixture : PatchNotesApiFixture
{
    private readonly TimeSpan _aiDelay;

    public SummarizeTestFixture(TimeSpan aiDelay)
    {
        _aiDelay = aiDelay;
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            // Replace IAiClient with a slow mock
            services.RemoveAll<IAiClient>();
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

    public async Task<string> SummarizeReleaseNotesAsync(string? releaseTitle, string? releaseBody, CancellationToken cancellationToken = default)
    {
        await Task.Delay(_delay, cancellationToken);
        return $"Mock summary of {releaseTitle}";
    }

    public async IAsyncEnumerable<string> SummarizeReleaseNotesStreamAsync(string? releaseTitle, string? releaseBody, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(_delay, cancellationToken);
        yield return "Mock ";
        yield return "streamed ";
        yield return "summary";
    }
}
