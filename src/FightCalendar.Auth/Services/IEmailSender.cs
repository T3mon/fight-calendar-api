namespace FightCalendar.Auth.Services;

// One interface for both purposes this service sends mail for -
// confirmation links now, event-reminder notifications later - so
// switching providers is a single new implementation, not a hunt through
// every call site.
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct);
}
