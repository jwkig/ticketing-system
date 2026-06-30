using System.Collections.Concurrent;
using TicketingSystem.Application.Services;

namespace TicketingSystem.Api.IntegrationTests;

/// <summary>
/// Test double for <see cref="IEmailService"/> that captures the verification
/// token instead of sending a real email, so tests can complete the verify flow.
/// </summary>
public sealed class FakeEmailService : IEmailService
{
    private readonly ConcurrentDictionary<string, string> _tokensByEmail = new(StringComparer.OrdinalIgnoreCase);

    public Task SendVerificationEmailAsync(string toEmail, string rawToken, CancellationToken ct = default)
    {
        _tokensByEmail[toEmail] = rawToken;
        return Task.CompletedTask;
    }

    public string? TokenFor(string email)
        => _tokensByEmail.TryGetValue(email, out var token) ? token : null;
}
