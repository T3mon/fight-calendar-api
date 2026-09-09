namespace FightCalendar.Web.Models.Domain;

public class Promotion
{
    public int Id { get; set; }

    // Tapology's short org code, e.g. "UFC", "ONE", "BKFC". Stable upsert key.
    public required string Code { get; set; }

    public required string Name { get; set; }

    public string? Website { get; set; }

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
