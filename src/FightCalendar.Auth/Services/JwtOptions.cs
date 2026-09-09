namespace FightCalendar.Auth.Services;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string SigningKey { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public int LifetimeDays { get; init; } = 30;
}
