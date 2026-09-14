namespace FightCalendar.Auth.Options;

// Where confirmation-email links point - the frontend shows a "you're
// confirmed" page and calls this service's own /auth/confirm-email, rather
// than the link hitting this JSON API directly.
public class FrontendOptions
{
    public const string SectionName = "Frontend";

    public required string BaseUrl { get; init; }
}
