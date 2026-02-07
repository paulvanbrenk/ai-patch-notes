using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PatchNotes.Data;
using PatchNotes.Data.GitHub;
using PatchNotes.Sync;

// Exit codes for cron monitoring
const int ExitSuccess = 0;
const int ExitPartialFailure = 1;
const int ExitFatalError = 2;

// Parse command-line arguments
var seedOnly = args.Contains("--seed");

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

builder.Services.AddTransient<ChangelogResolver>();
builder.Services.AddTransient<SyncService>();
builder.Services.AddTransient<NotificationSyncService>();

using var host = builder.Build();

// Get services
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var syncService = host.Services.GetRequiredService<SyncService>();

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

    logger.LogInformation("PatchNotes Sync starting");

    var result = await syncService.SyncAllAsync();

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
