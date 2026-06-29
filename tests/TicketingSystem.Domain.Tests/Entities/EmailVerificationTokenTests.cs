using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Domain.Tests.Entities;

public class EmailVerificationTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExpiresAt = Now.AddHours(24);

    private static EmailVerificationToken ValidToken() =>
        EmailVerificationToken.Create(Guid.NewGuid(), "hash123", ExpiresAt);

    [Fact]
    public void Use_ValidToken_SetsIsUsed()
    {
        var token = ValidToken();
        token.Use(Now);
        Assert.True(token.IsUsed);
    }

    [Fact]
    public void Use_AlreadyUsedToken_ThrowsDomainException()
    {
        var token = ValidToken();
        token.Use(Now);
        Assert.Throws<DomainException>(() => token.Use(Now));
    }

    [Fact]
    public void Use_ExpiredToken_ThrowsDomainException()
    {
        var token = ValidToken();
        var afterExpiry = ExpiresAt.AddSeconds(1);
        Assert.Throws<DomainException>(() => token.Use(afterExpiry));
    }

    [Fact]
    public void Use_AtExactExpiryMoment_ThrowsDomainException()
    {
        var token = ValidToken();
        Assert.Throws<DomainException>(() => token.Use(ExpiresAt));
    }

    [Fact]
    public void Create_SetsPropertiesCorrectly()
    {
        var userId = Guid.NewGuid();
        var token = EmailVerificationToken.Create(userId, "hash", ExpiresAt);

        Assert.Equal(userId, token.UserId);
        Assert.Equal("hash", token.TokenHash);
        Assert.Equal(ExpiresAt, token.ExpiresAt);
        Assert.False(token.IsUsed);
        Assert.NotEqual(Guid.Empty, token.Id);
    }
}
