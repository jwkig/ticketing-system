using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using TicketingSystem.Application.Services;
using TicketingSystem.Infrastructure.Options;

namespace TicketingSystem.Infrastructure.Services;

public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _smtp;
    private readonly AppSettings _app;

    public SmtpEmailService(IOptions<SmtpSettings> smtp, IOptions<AppSettings> app)
    {
        _smtp = smtp.Value;
        _app = app.Value;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string rawToken, CancellationToken ct = default)
    {
        // Links to the SPA's verification screen (not the API directly), which
        // then calls GET /api/auth/verify-email and renders the result.
        var verifyUrl = $"{_app.BaseUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(rawToken)}";

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse("noreply@ticketing.app"));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Verify your email address";
        message.Body = new TextPart("plain")
        {
            Text = $"Click the link below to verify your email address:\n\n{verifyUrl}\n\nThis link expires in 24 hours."
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.Auto, ct);

        if (!string.IsNullOrEmpty(_smtp.User))
            await client.AuthenticateAsync(_smtp.User, _smtp.Password, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(quit: true, ct);
    }
}
