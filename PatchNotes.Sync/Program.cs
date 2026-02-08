using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PatchNotes.Data;
using PatchNotes.Data.AI;
using PatchNotes.Data.GitHub;
using PatchNotes.Sync;

// Exit codes for cron monitoring
const int ExitSuccess = 0;
const int ExitPartialFailure = 1;
const int ExitFatalError = 2;

// Parse command-line arguments
var seedOnly = args.Contains("--seed");
var summarizeRepo = GetArgValue(args, "-s");
var repoIndex = Array.IndexOf(args, "-r");
string? repoUrl = repoIndex >= 0 && repoIndex + 1 < args.Length ? args[repoIndex + 1] : null;

static string? GetArgValue(string[] args, string flag)
{
    var index = Array.IndexOf(args, flag);
    if (index >= 0 && index + 1 < args.Length)
        return args[index + 1];
    return null;
}

var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(
    builder.Environment.IsDevelopment() ? LogLevel.Debug : LogLevel.Information);

// Configure services
builder.Services.AddPatchNotesDbContext(builder.Configuration);

builder.Services.AddGitHubClient(options =>
{
    var token = builder.Configuration["GitHub:Token"];
    if (!string.IsNullOrEmpty(token))
    {
        options.Token = token;
    }
});

builder.Services.AddAiClient(options =>
{
    builder.Configuration.GetSection(AiClientOptions.SectionName).Bind(options);
});

builder.Services.AddTransient<ChangelogResolver>();
builder.Services.AddTransient<VersionGroupingService>();
builder.Services.AddTransient<SyncService>();
builder.Services.AddTransient<NotificationSyncService>();
builder.Services.AddTransient<SummaryGenerationService>();

using var host = builder.Build();

// Get services
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var syncService = host.Services.GetRequiredService<SyncService>();
var summaryService = host.Services.GetRequiredService<SummaryGenerationService>();

try
{
    // Handle --seed flag
    if (seedOnly)
    {
        logger.LogInformation("Seeding database with sample data");
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);
        logger.LogInformation("Database seeded successfully");
        return ExitSuccess;
    }

    // Handle -s <owner/repo> flag: generate summaries for a specific package
    if (summarizeRepo != null)
    {
        var parts = summarizeRepo.Split('/');
        if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
        {
            logger.LogError("Invalid repository format. Expected: owner/repo (e.g., prettier/prettier)");
            return ExitFatalError;
        }

        var owner = parts[0];
        var repo = parts[1];

        logger.LogInformation("Generating summaries for {Owner}/{Repo}", owner, repo);

        var db = host.Services.GetRequiredService<PatchNotesDbContext>();

        var package = await db.Packages
            .FirstOrDefaultAsync(p => p.GithubOwner == owner && p.GithubRepo == repo);

        if (package == null)
        {
            logger.LogError(
                "Package {Owner}/{Repo} not found. Run 'sync -r' first to fetch the package and its releases.",
                owner, repo);
            return ExitFatalError;
        }

        var summaryResult = await summaryService.GenerateGroupSummariesAsync(package.Id);

        logger.LogInformation(
            "Summary generation for {Owner}/{Repo} complete: {Generated} generated, {Skipped} skipped, {Errors} errors",
            owner, repo,
            summaryResult.SummariesGenerated,
            summaryResult.GroupsSkipped,
            summaryResult.Errors.Count);

        return summaryResult.Success ? ExitSuccess : ExitPartialFailure;
    }

    // Handle -r <github-url> flag
    if (repoUrl != null)
    {
        var (owner, repo) = GitHubUrlParser.Parse(repoUrl);
        logger.LogInformation("Syncing repository {Owner}/{Repo}", owner, repo);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        await db.Database.MigrateAsync();

        var scopedSyncService = scope.ServiceProvider.GetRequiredService<SyncService>();
        var repoResult = await scopedSyncService.SyncRepoAsync(owner, repo);
        logger.LogInformation(
            "Sync complete: {Releases} releases fetched",
            repoResult.ReleasesAdded);
        return ExitSuccess;
    }

    logger.LogInformation("PatchNotes Sync starting");

    // Backfill denormalized version fields for any existing releases
    var backfilled = await syncService.BackfillVersionFieldsAsync();
    if (backfilled > 0)
    {
        logger.LogInformation("Backfilled version fields for {Count} existing releases", backfilled);
    }

    var result = await syncService.SyncAllAsync();

    // Generate summaries for packages with new/stale releases
    if (result.ReleasesNeedingSummary.Count > 0)
    {
        logger.LogInformation(
            "Generating summaries for {Count} releases needing summaries",
            result.ReleasesNeedingSummary.Count);

        var summaryResult = await summaryService.GenerateAllSummariesAsync();

        logger.LogInformation(
            "Summary generation complete: {Generated} generated, {Skipped} skipped, {Errors} errors",
            summaryResult.SummariesGenerated,
            summaryResult.GroupsSkipped,
            summaryResult.Errors.Count);
    }

    if (result.Success)
    {
        logger.LogInformation(
            "Sync completed successfully: {Packages} packages, {Releases} releases",
            result.PackagesSynced,
            result.ReleasesAdded);
        return ExitSuccess;
    }
    else
    {
        logger.LogWarning(
            "Sync completed with {ErrorCount} errors",
            result.Errors.Count);
        foreach (var error in result.Errors)
        {
            logger.LogWarning("  {Package}: {Message}", error.PackageName, error.Message);
        }
        return ExitPartialFailure;
    }
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Fatal error during sync");
    return ExitFatalError;
}
