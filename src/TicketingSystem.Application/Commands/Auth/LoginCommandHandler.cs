using MediatR;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Commands.Auth;

public class LoginCommandHandler : IRequestHandler<LoginCommand, JwtTokenDto>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;

    public LoginCommandHandler(IUserRepository users, IPasswordHasher hasher, IJwtService jwt)
    {
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<JwtTokenDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = new EmailAddress(request.Email);

        var user = await _users.GetByEmailAsync(email, cancellationToken);
        if (user is null || !_hasher.Verify(user.PasswordHash, request.Password))
            throw new DomainException("Invalid credentials.");

        if (!user.IsEmailVerified)
            throw new DomainException("Email address has not been verified.");

        var token = _jwt.GenerateToken(user.Id, user.Email.Value);
        return new JwtTokenDto(token);
    }
}
