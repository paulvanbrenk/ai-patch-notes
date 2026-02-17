using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PatchNotes.Sync;

namespace PatchNotes.Functions;

public class SyncTimerFunction(
    SyncPipeline pipeline,
    ILogger<SyncTimerFunction> logger)
{
    // Runs every 6 hours: at midnight, 6am, noon, 6pm UTC
    [Function("SyncReleases")]
    public async Task Run(
        [TimerTrigger("0 0 */6 * * *")] TimerInfo timerInfo,
        CancellationToken cancellationToken)
    {
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogWarning("SyncReleases started at {Time}, IsPastDue: {IsPastDue}",
            startedAt, timerInfo.IsPastDue);

        try
        {
            var result = await pipeline.RunAsync(cancellationToken);

            var elapsed = DateTimeOffset.UtcNow - startedAt;
            logger.LogWarning(
                "SyncReleases completed in {ElapsedSeconds:F1}s — " +
                "{Packages} packages ({PackagesWithNewReleases} with new releases), {Releases} new releases, " +
                "{Summaries} summaries generated, {SyncErrors} sync errors, {SummaryErrors} summary errors",
                elapsed.TotalSeconds,
                result.PackagesSynced,
                result.PackagesWithNewReleases,
                result.ReleasesAdded,
                result.SummariesGenerated,
                result.SyncErrors.Count,
                result.SummaryErrors.Count);

            foreach (var error in result.SyncErrors)
            {
                logger.LogError("Sync error for {Package}: {Error}", error.PackageName, error.Message);
            }

            foreach (var error in result.SummaryErrors)
            {
                logger.LogError("Summary error for package {PackageId}: {Error}", error.PackageId, error.Message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SyncReleases failed after {ElapsedSeconds:F1}s",
                (DateTimeOffset.UtcNow - startedAt).TotalSeconds);
            throw;
        }

        if (timerInfo.ScheduleStatus is not null)
        {
            logger.LogWarning("Next SyncReleases scheduled at {NextRun}", timerInfo.ScheduleStatus.Next);
        }
    }
}
