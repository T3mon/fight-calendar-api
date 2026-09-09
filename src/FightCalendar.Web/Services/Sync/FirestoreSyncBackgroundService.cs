using FightCalendar.Web.Services.Firestore;
using Microsoft.Extensions.Options;

namespace FightCalendar.Web.Services.Sync;

// Runs EventSyncRunner once at startup and then on a fixed interval
// (SyncIntervalHours, default 12h) for the lifetime of the app. The
// underlying Firestore data only changes as often as someone runs the
// scraper (roughly weekly), so this is deliberately a low-frequency poll,
// not a tight loop.
public class FirestoreSyncBackgroundService(IServiceScopeFactory scopeFactory, IOptions<FirestoreOptions> options, ILogger<FirestoreSyncBackgroundService> logger) : BackgroundService
{
    private readonly FirestoreOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(_options.SyncIntervalHours));

        do
        {
            await RunOnceAsync(stoppingToken);
        } while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<EventSyncRunner>();
            await runner.RunAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Scheduled Firestore event sync failed");
        }
    }
}
