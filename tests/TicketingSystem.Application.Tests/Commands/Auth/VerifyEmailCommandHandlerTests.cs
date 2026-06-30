using System.Security.Cryptography;
using System.Text;
using TicketingSystem.Application.Commands.Auth;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Tests.Commands.Auth;

public class VerifyEmailCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExpiresAt = Now.AddHours(24);

    private readonly Mock<IVerificationTokenRepository> _tokens = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly VerifyEmailCommandHandler _sut;

    public VerifyEmailCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
        _sut = new VerifyEmailCommandHandler(_tokens.Object, _users.Object, _clock.Object, _uow.Object);
    }

    private static string AnyHash() => "AABBCCDDEEFF00112233445566778899AABBCCDDEEFF00112233445566778899";

    [Fact]
    public async Task Handle_ValidToken_VerifiesUserEmailAndSaves()
    {
        var userId = Guid.NewGuid();
        var token = EmailVerificationToken.Create(userId, AnyHash(), ExpiresAt);
        var user = User.Create(new EmailAddress("user@example.com"), "hash", Now);

        _tokens.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(token);
        _users.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);

        await _sut.Handle(new VerifyEmailCommand("any-raw-token"), default);

        Assert.True(user.IsEmailVerified);
        Assert.True(token.IsUsed);
        _users.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TokenNotFound_ThrowsDomainException()
    {
        _tokens.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((EmailVerificationToken?)null);

        await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new VerifyEmailCommand("invalid-token"), default));
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsDomainException()
    {
        var token = EmailVerificationToken.Create(Guid.NewGuid(), AnyHash(), Now.AddHours(-1));

        _tokens.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(token);

        await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new VerifyEmailCommand("expired-token"), default));
    }

    [Fact]
    public async Task Handle_AlreadyUsedToken_ThrowsDomainException()
    {
        var token = EmailVerificationToken.Create(Guid.NewGuid(), AnyHash(), ExpiresAt);
        token.Use(Now);

        _tokens.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(token);

        await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new VerifyEmailCommand("used-token"), default));
    }

    [Fact]
    public async Task Handle_ValidToken_LooksUpTokenByHashOfRawInput()
    {
        var rawToken = "my-raw-token";
        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        _tokens.Setup(x => x.GetByTokenHashAsync(expectedHash, It.IsAny<CancellationToken>()))
               .ReturnsAsync((EmailVerificationToken?)null);

        await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new VerifyEmailCommand(rawToken), default));

        _tokens.Verify(x => x.GetByTokenHashAsync(expectedHash, It.IsAny<CancellationToken>()), Times.Once);
    }
}
