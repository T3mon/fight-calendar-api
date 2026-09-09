namespace FightCalendar.Web.Services.Firestore;

// Shape of what the tapology-firebase-scraper project writes into Firestore
// (see that repo's README for the canonical example document).
public class TapologyEventDto
{
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string Link { get; init; }
    public required string Organization { get; init; }
    public required string FullOrganization { get; init; }

    // Raw Tapology string, e.g. "Saturday 09.08.2026 at 07:00 PM ET".
    public required string Date { get; init; }

    public string? Venue { get; init; }
    public string? Location { get; init; }

    public List<TapologyFightDto> Fights { get; init; } = [];
}

public class TapologyFightDto
{
    public required TapologyFighterDto FighterA { get; init; }
    public required TapologyFighterDto FighterB { get; init; }
    public string? WeightClass { get; init; }
}

public class TapologyFighterDto
{
    public required string Name { get; init; }
    public string? Record { get; init; }
    public required string Link { get; init; }
}
