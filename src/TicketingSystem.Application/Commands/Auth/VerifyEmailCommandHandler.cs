using System.Security.Cryptography;
using System.Text;
using MediatR;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Application.Commands.Auth;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand>
{
    private readonly IVerificationTokenRepository _tokens;
    private readonly IUserRepository _users;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;

    public VerifyEmailCommandHandler(
        IVerificationTokenRepository tokens,
        IUserRepository users,
        IDateTimeProvider clock,
        IUnitOfWork uow)
    {
        _tokens = tokens;
        _users = users;
        _clock = clock;
        _uow = uow;
    }

    public async Task Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = ComputeHash(request.Token);

        var verificationToken = await _tokens.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (verificationToken is null)
            throw new DomainException("Invalid or expired verification token.");

        verificationToken.Use(_clock.UtcNow);

        var user = await _users.GetByIdAsync(verificationToken.UserId, cancellationToken)
            ?? throw new NotFoundException($"User {verificationToken.UserId} not found.");

        user.VerifyEmail();
        await _users.UpdateAsync(user, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
    }

    private static string ComputeHash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
