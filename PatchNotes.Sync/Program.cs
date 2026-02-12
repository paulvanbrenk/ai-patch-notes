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
var initSync = args.Contains("--init");
var hasSFlag = Array.IndexOf(args, "-s") >= 0;
var summarizeRepo = GetArgValue(args, "-s");
var hasRFlag = Array.IndexOf(args, "-r") >= 0;
var repoIndex = Array.IndexOf(args, "-r");
string? repoUrl = repoIndex >= 0 && repoIndex + 1 < args.Length ? args[repoIndex + 1] : null;

// Validate: if -s or -r flag is present but missing its required value, show error
if (hasSFlag && summarizeRepo == null)
{
    Console.Error.WriteLine("Error: -s flag requires a value. Usage: sync -s <owner/repo>");
    return ExitFatalError;
}
if (hasRFlag && repoUrl == null)
{
    Console.Error.WriteLine("Error: -r flag requires a value. Usage: sync -r <github-url>");
    return ExitFatalError;
}

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
builder.Services.AddTransient<SummaryGenerationService>();
builder.Services.AddTransient<SyncPipeline>();

using var host = builder.Build();

// Get singleton services from root provider (scoped services resolved per code path below)
var logger = host.Services.GetRequiredService<ILogger<Program>>();

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

    // Handle --init flag: seed package catalog from packages.json, then sync all from GitHub
    if (initSync)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        await db.Database.MigrateAsync();

        var added = await DbSeeder.SeedPackageCatalogAsync(db);
        if (added > 0)
            logger.LogInformation("Added {Count} packages from seed catalog", added);
        else
            logger.LogInformation("All seed packages already exist in database");

        var pipeline = host.Services.GetRequiredService<SyncPipeline>();
        var result = await pipeline.RunAsync();

        logger.LogInformation(
            "Init complete: {Packages} packages synced, {Releases} releases added, " +
            "{Summaries} summaries generated",
            result.PackagesSynced, result.ReleasesAdded, result.SummariesGenerated);

        return result.Success ? ExitSuccess : ExitPartialFailure;
    }

    // Handle -s <owner/repo> flag: generate summaries for a specific package
    if (summarizeRepo != null)
    {
        string owner, repo;
        try
        {
            (owner, repo) = GitHubUrlParser.Parse(summarizeRepo);
        }
        catch (ArgumentException)
        {
            logger.LogError("Invalid repository format. Expected: owner/repo (e.g., prettier/prettier)");
            return ExitFatalError;
        }

        logger.LogInformation("Generating summaries for {Owner}/{Repo}", owner, repo);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var summaryService = scope.ServiceProvider.GetRequiredService<SummaryGenerationService>();

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
        var syncService = scope.ServiceProvider.GetRequiredService<SyncService>();
        await db.Database.MigrateAsync();

        try
        {
            var repoResult = await syncService.SyncRepoAsync(owner, repo);
            logger.LogInformation(
                "Sync complete: {Releases} releases fetched",
                repoResult.ReleasesAdded);
            return ExitSuccess;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sync repository {Owner}/{Repo}", owner, repo);
            return ExitPartialFailure;
        }
    }

    logger.LogInformation("PatchNotes Sync starting");

    {
        var pipeline = host.Services.GetRequiredService<SyncPipeline>();
        var result = await pipeline.RunAsync();

        logger.LogInformation(
            "Pipeline complete: {Packages} packages synced, {Releases} releases added, " +
            "{Summaries} summaries generated",
            result.PackagesSynced,
            result.ReleasesAdded,
            result.SummariesGenerated);

        if (result.Success)
        {
            return ExitSuccess;
        }
        else
        {
            foreach (var error in result.SyncErrors)
            {
                logger.LogWarning("  Sync error — {Package}: {Message}", error.PackageName, error.Message);
            }
            foreach (var error in result.SummaryErrors)
            {
                logger.LogWarning("  Summary error — {PackageId}: {Message}", error.PackageId, error.Message);
            }
            return ExitPartialFailure;
        }
    }
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Fatal error during sync");
    return ExitFatalError;
}
