using TicketingSystem.Application.Commands.Auth;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Tests.Commands.Auth;

public class ResendVerificationCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IVerificationTokenRepository> _tokens = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly ResendVerificationCommandHandler _sut;

    public ResendVerificationCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
        _sut = new ResendVerificationCommandHandler(
            _users.Object, _tokens.Object, _email.Object, _clock.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_UnverifiedUser_CreatesNewTokenAndSendsEmail()
    {
        var user = User.Create(new EmailAddress("user@example.com"), "hash", Now);
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);

        await _sut.Handle(new ResendVerificationCommand("user@example.com"), default);

        _tokens.Verify(x => x.InvalidatePreviousForUserAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
        _tokens.Verify(x => x.AddAsync(It.IsAny<EmailVerificationToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _email.Verify(x => x.SendVerificationEmailAsync("user@example.com", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailNotRegistered_SilentlyReturns()
    {
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((User?)null);

        await _sut.Handle(new ResendVerificationCommand("ghost@example.com"), default);

        _email.Verify(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyVerifiedUser_SilentlyReturns()
    {
        var user = User.Create(new EmailAddress("user@example.com"), "hash", Now);
        user.VerifyEmail();
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);

        await _sut.Handle(new ResendVerificationCommand("user@example.com"), default);

        _email.Verify(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnverifiedUser_InvalidatesPreviousTokensBeforeAddingNew()
    {
        var user = User.Create(new EmailAddress("user@example.com"), "hash", Now);
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);

        var callOrder = new List<string>();
        _tokens.Setup(x => x.InvalidatePreviousForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .Callback<Guid, CancellationToken>((_, _) => callOrder.Add("invalidate"))
               .Returns(Task.CompletedTask);
        _tokens.Setup(x => x.AddAsync(It.IsAny<EmailVerificationToken>(), It.IsAny<CancellationToken>()))
               .Callback<EmailVerificationToken, CancellationToken>((_, _) => callOrder.Add("add"))
               .Returns(Task.CompletedTask);

        await _sut.Handle(new ResendVerificationCommand("user@example.com"), default);

        Assert.Equal(["invalidate", "add"], callOrder);
    }

    [Fact]
    public async Task Handle_UnverifiedUser_NewTokenExpiresIn24Hours()
    {
        var user = User.Create(new EmailAddress("user@example.com"), "hash", Now);
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(user);

        EmailVerificationToken? captured = null;
        _tokens.Setup(x => x.AddAsync(It.IsAny<EmailVerificationToken>(), It.IsAny<CancellationToken>()))
               .Callback<EmailVerificationToken, CancellationToken>((t, _) => captured = t)
               .Returns(Task.CompletedTask);

        await _sut.Handle(new ResendVerificationCommand("user@example.com"), default);

        Assert.NotNull(captured);
        Assert.Equal(Now.AddHours(24), captured.ExpiresAt);
    }
}
