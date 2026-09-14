namespace WhoFights.Auth.Options;

public class GoogleOptions
{
    public const string SectionName = "Google";

    public required string ClientId { get; init; }
}
