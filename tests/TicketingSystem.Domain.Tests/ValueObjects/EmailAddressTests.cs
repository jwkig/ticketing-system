using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Domain.Tests.ValueObjects;

public class EmailAddressTests
{
    [Theory]
    [InlineData("User@Example.COM", "user@example.com")]
    [InlineData("  alice@domain.org  ", "alice@domain.org")]
    [InlineData("bob+tag@sub.domain.io", "bob+tag@sub.domain.io")]
    public void Constructor_NormalisesAndTrims(string input, string expected)
    {
        var email = new EmailAddress(input);
        Assert.Equal(expected, email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyOrWhitespace_ThrowsDomainException(string input)
    {
        Assert.Throws<DomainException>(() => new EmailAddress(input));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing-at-sign")]
    [InlineData("@nodomain")]
    [InlineData("noat.com")]
    public void Constructor_InvalidFormat_ThrowsDomainException(string input)
    {
        Assert.Throws<DomainException>(() => new EmailAddress(input));
    }

    [Fact]
    public void Constructor_NullInput_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new EmailAddress(null!));
    }

    [Fact]
    public void EqualityIsValueBased()
    {
        var a = new EmailAddress("User@Example.com");
        var b = new EmailAddress("user@example.com");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ToString_ReturnsNormalisedValue()
    {
        var email = new EmailAddress("Test@Example.COM");
        Assert.Equal("test@example.com", email.ToString());
    }
}
