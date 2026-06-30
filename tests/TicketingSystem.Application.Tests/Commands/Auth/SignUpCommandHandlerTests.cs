using TicketingSystem.Application.Commands.Auth;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Tests.Commands.Auth;

public class SignUpCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IVerificationTokenRepository> _tokens = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly SignUpCommandHandler _sut;

    public SignUpCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
        _hasher.Setup(x => x.Hash(It.IsAny<string>())).Returns("hashed-password");
        _sut = new SignUpCommandHandler(
            _users.Object, _tokens.Object, _hasher.Object,
            _email.Object, _clock.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_NewEmail_CreatesUserAndSendsVerificationEmail()
    {
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((User?)null);

        await _sut.Handle(new SignUpCommand("new@example.com", "Password1!"), default);

        _users.Verify(x => x.AddAsync(It.Is<User>(u => u.Email.Value == "new@example.com"), It.IsAny<CancellationToken>()), Times.Once);
        _tokens.Verify(x => x.AddAsync(It.IsAny<EmailVerificationToken>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _email.Verify(x => x.SendVerificationEmailAsync("new@example.com", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingEmail_ThrowsDomainException()
    {
        var existing = User.Create(new EmailAddress("taken@example.com"), "hash", Now);
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(existing);

        await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new SignUpCommand("taken@example.com", "Password1!"), default));
    }

    [Fact]
    public async Task Handle_NewUser_InvalidatesPreviousTokensBeforeAddingNew()
    {
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((User?)null);

        var callOrder = new List<string>();
        _tokens.Setup(x => x.InvalidatePreviousForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .Callback<Guid, CancellationToken>((_, _) => callOrder.Add("invalidate"))
               .Returns(Task.CompletedTask);
        _tokens.Setup(x => x.AddAsync(It.IsAny<EmailVerificationToken>(), It.IsAny<CancellationToken>()))
               .Callback<EmailVerificationToken, CancellationToken>((_, _) => callOrder.Add("add"))
               .Returns(Task.CompletedTask);

        await _sut.Handle(new SignUpCommand("user@example.com", "Password1!"), default);

        Assert.Equal(["invalidate", "add"], callOrder);
    }

    [Fact]
    public async Task Handle_NewUser_TokenExpiresIn24Hours()
    {
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((User?)null);

        EmailVerificationToken? captured = null;
        _tokens.Setup(x => x.AddAsync(It.IsAny<EmailVerificationToken>(), It.IsAny<CancellationToken>()))
               .Callback<EmailVerificationToken, CancellationToken>((t, _) => captured = t)
               .Returns(Task.CompletedTask);

        await _sut.Handle(new SignUpCommand("user@example.com", "Password1!"), default);

        Assert.NotNull(captured);
        Assert.Equal(Now.AddHours(24), captured.ExpiresAt);
    }

    [Fact]
    public async Task Handle_NewUser_PasswordIsHashed()
    {
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((User?)null);

        User? captured = null;
        _users.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
              .Callback<User, CancellationToken>((u, _) => captured = u)
              .Returns(Task.CompletedTask);

        await _sut.Handle(new SignUpCommand("user@example.com", "Password1!"), default);

        Assert.NotNull(captured);
        Assert.Equal("hashed-password", captured.PasswordHash);
        _hasher.Verify(x => x.Hash("Password1!"), Times.Once);
    }

    [Fact]
    public async Task Handle_SaveChangesCalledBeforeSendingEmail()
    {
        _users.Setup(x => x.GetByEmailAsync(It.IsAny<EmailAddress>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((User?)null);

        var callOrder = new List<string>();
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(_ => callOrder.Add("save"))
            .Returns(Task.CompletedTask);
        _email.Setup(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Callback<string, string, CancellationToken>((_, _, _) => callOrder.Add("email"))
              .Returns(Task.CompletedTask);

        await _sut.Handle(new SignUpCommand("user@example.com", "Password1!"), default);

        Assert.Equal(["save", "email"], callOrder);
    }
}
