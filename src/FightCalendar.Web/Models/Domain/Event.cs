namespace FightCalendar.Web.Models.Domain;

public class Event
{
    public int Id { get; set; }

    // Tapology's URL slug (also the Firestore document id). Stable upsert key.
    public required string TapologySlug { get; set; }

    public required string Title { get; set; }

    public int PromotionId { get; set; }
    public Promotion Promotion { get; set; } = null!;

    public DateTimeOffset StartsAt { get; set; }

    public string? Venue { get; set; }
    public string? Location { get; set; }

    public required string TapologyLink { get; set; }

    public ICollection<Bout> Bouts { get; set; } = new List<Bout>();
}
