using System.Security.Cryptography;
using System.Text;
using MediatR;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Commands.Auth;

public class SignUpCommandHandler : IRequestHandler<SignUpCommand>
{
    private readonly IUserRepository _users;
    private readonly IVerificationTokenRepository _tokens;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailService _email;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;

    public SignUpCommandHandler(
        IUserRepository users,
        IVerificationTokenRepository tokens,
        IPasswordHasher hasher,
        IEmailService email,
        IDateTimeProvider clock,
        IUnitOfWork uow)
    {
        _users = users;
        _tokens = tokens;
        _hasher = hasher;
        _email = email;
        _clock = clock;
        _uow = uow;
    }

    public async Task Handle(SignUpCommand request, CancellationToken cancellationToken)
    {
        var email = new EmailAddress(request.Email);

        var existing = await _users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
            throw new DomainException("Email is already registered.");

        var passwordHash = _hasher.Hash(request.Password);
        var user = User.Create(email, passwordHash, _clock.UtcNow);
        await _users.AddAsync(user, cancellationToken);

        var rawToken = GenerateRawToken();
        var tokenHash = ComputeHash(rawToken);

        await _tokens.InvalidatePreviousForUserAsync(user.Id, cancellationToken);
        var verificationToken = EmailVerificationToken.Create(user.Id, tokenHash, _clock.UtcNow.AddHours(24));
        await _tokens.AddAsync(verificationToken, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        await _email.SendVerificationEmailAsync(email.Value, rawToken, cancellationToken);
    }

    internal static string GenerateRawToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    internal static string ComputeHash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
