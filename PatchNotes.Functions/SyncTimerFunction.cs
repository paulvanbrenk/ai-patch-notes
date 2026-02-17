using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PatchNotes.Sync.Core;

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
        logger.LogInformation("Sync function triggered at {Time}", DateTimeOffset.UtcNow);

        var result = await pipeline.RunAsync(cancellationToken);

        logger.LogInformation(
            "Sync pipeline complete: {Packages} packages synced, {Releases} releases added, " +
            "{Summaries} summaries generated, {SyncErrors} sync errors, {SummaryErrors} summary errors",
            result.PackagesSynced,
            result.ReleasesAdded,
            result.SummariesGenerated,
            result.SyncErrors.Count,
            result.SummaryErrors.Count);

        if (timerInfo.ScheduleStatus is not null)
        {
            logger.LogInformation("Next sync scheduled at {NextRun}", timerInfo.ScheduleStatus.Next);
        }
    }
}
