using FightCalendar.Data.Services.Firestore;

namespace FightCalendar.Data.Services.Sync;

// Thin orchestrator: fetch from Firestore, hand off to EventSyncService.
// Scoped so it can be resolved fresh both from the periodic background
// service and from an on-demand admin action.
public class EventSyncRunner(FirestoreEventsClient firestoreClient, EventSyncService syncService, ILogger<EventSyncRunner> logger)
{
    public async Task<EventSyncResult> RunAsync(CancellationToken ct = default)
    {
        var events = await firestoreClient.FetchAllEventsAsync(ct);
        var result = await syncService.SyncAsync(events, ct);

        logger.LogInformation(
            "Event sync complete: {Fetched} fetched, {Created} created, {Updated} updated, {Skipped} skipped",
            events.Count, result.Created, result.Updated, result.Skipped);

        return result;
    }
}
