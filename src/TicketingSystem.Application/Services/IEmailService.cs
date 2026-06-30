namespace TicketingSystem.Application.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string rawToken, CancellationToken ct = default);
}
