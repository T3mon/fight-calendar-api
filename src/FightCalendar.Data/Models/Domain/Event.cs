namespace FightCalendar.Data.Models.Domain;

public class Event
{
    public int Id { get; set; }

    // Tapology's URL slug (also the Firestore document id). Stable upsert key.
    public required string TapologySlug { get; set; }

    public required string Title { get; set; }

    // Derived from Title at sync time (see EventSeriesClassifier) - not
    // scraped data. Null means this is a flagship/numbered event with no
    // named sub-series (e.g. "UFC 331"), as opposed to e.g. "Fight Night"
    // or "Friday Fights".
    public string? SubSeries { get; set; }

    public int PromotionId { get; set; }
    public Promotion Promotion { get; set; } = null!;

    public DateTimeOffset StartsAt { get; set; }

    public string? Venue { get; set; }
    public string? Location { get; set; }

    public required string TapologyLink { get; set; }

    public ICollection<Bout> Bouts { get; set; } = new List<Bout>();
}
