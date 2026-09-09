namespace FightCalendar.Web.Models.Domain;

public class Bout
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    // Position in Tapology's fight array; 0 is the main event.
    public int OrderIndex { get; set; }

    public long FighterAId { get; set; }
    public Fighter FighterA { get; set; } = null!;

    public long FighterBId { get; set; }
    public Fighter FighterB { get; set; } = null!;

    public string? WeightClass { get; set; }
}
