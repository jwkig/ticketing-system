using TicketingSystem.Application.Commands.Auth;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Tests.Commands.Auth;

public class LoginCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        _jwt.Setup(x => x.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>())).Returns("jwt-token");
        _sut = new LoginCommandHandler(_users.Object, _hasher.Object, _jwt.Object);
    }

    private User VerifiedUser(string email = "user@example.com")
    {
        var user = User.Create(new EmailAddress(email), "hash", Now);
        user.VerifyEmail();
        return user;
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsJwtToken()
    {
        var user = VerifiedUser();
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _hasher.Setup(x => x.Verify("hash", "correct-password")).Returns(true);

        var result = await _sut.Handle(new LoginCommand("user@example.com", "correct-password"), default);

        Assert.Equal("jwt-token", result.Token);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsDomainException()
    {
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new LoginCommand("ghost@example.com", "Password1!"), default));
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsDomainException()
    {
        var user = VerifiedUser();
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _hasher.Setup(x => x.Verify("hash", It.IsAny<string>())).Returns(false);

        await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new LoginCommand("user@example.com", "wrong-password"), default));
    }

    [Fact]
    public async Task Handle_EmailNotVerified_ThrowsDomainException()
    {
        var user = User.Create(new EmailAddress("user@example.com"), "hash", Now);
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _hasher.Setup(x => x.Verify("hash", It.IsAny<string>())).Returns(true);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new LoginCommand("user@example.com", "Password1!"), default));

        Assert.Contains("verified", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ValidCredentials_GeneratesTokenWithCorrectClaims()
    {
        var user = VerifiedUser("user@example.com");
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);
        _hasher.Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        await _sut.Handle(new LoginCommand("user@example.com", "Password1!"), default);

        _jwt.Verify(x => x.GenerateToken(user.Id, "user@example.com"), Times.Once);
    }
}
