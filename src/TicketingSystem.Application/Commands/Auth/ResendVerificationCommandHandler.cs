using System.Security.Cryptography;
using System.Text;
using MediatR;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Commands.Auth;

public class ResendVerificationCommandHandler : IRequestHandler<ResendVerificationCommand>
{
    private readonly IUserRepository _users;
    private readonly IVerificationTokenRepository _tokens;
    private readonly IEmailService _email;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;

    public ResendVerificationCommandHandler(
        IUserRepository users,
        IVerificationTokenRepository tokens,
        IEmailService email,
        IDateTimeProvider clock,
        IUnitOfWork uow)
    {
        _users = users;
        _tokens = tokens;
        _email = email;
        _clock = clock;
        _uow = uow;
    }

    public async Task Handle(ResendVerificationCommand request, CancellationToken cancellationToken)
    {
        var email = new EmailAddress(request.Email);
        var user = await _users.GetByEmailAsync(email, cancellationToken);

        // Silent return: don't reveal whether the email is registered or verified
        if (user is null || user.IsEmailVerified)
            return;

        var rawToken = GenerateRawToken();
        var tokenHash = ComputeHash(rawToken);

        await _tokens.InvalidatePreviousForUserAsync(user.Id, cancellationToken);
        var verificationToken = EmailVerificationToken.Create(user.Id, tokenHash, _clock.UtcNow.AddHours(24));
        await _tokens.AddAsync(verificationToken, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        await _email.SendVerificationEmailAsync(email.Value, rawToken, cancellationToken);
    }

    private static string GenerateRawToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private static string ComputeHash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
