using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PatchNotes.Data;
using PatchNotes.Sync.Core;

namespace PatchNotes.Functions;

public class SyncNewPackagesFunction(
    IServiceScopeFactory scopeFactory,
    ILogger<SyncNewPackagesFunction> logger)
{
    [Function("SyncNewPackages")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "sync-new-packages")]
        HttpRequest req,
        CancellationToken ct)
    {
        logger.LogInformation("SyncNewPackages triggered");

        var processed = 0;

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PatchNotesDbContext>();
        var syncService = scope.ServiceProvider.GetRequiredService<SyncService>();
        var summaryService = scope.ServiceProvider.GetRequiredService<SummaryGenerationService>();

        var newPackages = await db.Packages
            .Where(p => p.LastFetchedAt == null)
            .ToListAsync(ct);

        logger.LogInformation("Found {Count} never-synced packages", newPackages.Count);

        foreach (var package in newPackages)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var syncResult = await syncService.SyncPackageAsync(package, cancellationToken: ct);
                logger.LogInformation(
                    "Synced new package {Package}: {Releases} releases",
                    package.Name, syncResult.ReleasesAdded);

                if (syncResult.ReleasesNeedingSummary.Count > 0)
                {
                    var summaryResult = await summaryService.GenerateGroupSummariesAsync(package.Id, ct);
                    logger.LogInformation(
                        "Generated {Count} summaries for new package {Package}",
                        summaryResult.SummariesGenerated, package.Name);
                }

                processed++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sync new package {Package}", package.Name);
            }
        }

        logger.LogInformation("SyncNewPackages completed: {Processed} packages processed", processed);
        return new OkObjectResult(new { processed });
    }
}
