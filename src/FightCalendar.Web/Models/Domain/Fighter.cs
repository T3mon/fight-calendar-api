namespace FightCalendar.Web.Models.Domain;

public class Fighter
{
    // Tapology's own numeric fighter ID, parsed from their profile URL.
    // Using their ID instead of the name avoids collisions between fighters
    // who share a name (Tapology's own data has duplicates on the same card).
    public long Id { get; set; }

    public required string Name { get; set; }

    public string? Record { get; set; }

    public string? CountryFlagUrl { get; set; }

    public string? PictureUrl { get; set; }

    public required string TapologyLink { get; set; }
}
