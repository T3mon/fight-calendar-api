namespace FightCalendar.Data.Services.Firestore;

public class FirestoreOptions
{
    public const string SectionName = "Firestore";

    public required string ProjectId { get; init; }
    public required string CollectionName { get; init; }
    public int SyncIntervalHours { get; init; } = 12;
}
