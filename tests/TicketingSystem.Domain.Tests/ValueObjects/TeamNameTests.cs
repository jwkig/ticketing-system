using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Domain.Tests.ValueObjects;

public class TeamNameTests
{
    [Theory]
    [InlineData("  Backend  ", "Backend")]
    [InlineData("Frontend", "Frontend")]
    [InlineData("  QA Team  ", "QA Team")]
    public void Constructor_TrimsWhitespace(string input, string expected)
    {
        var name = new TeamName(input);
        Assert.Equal(expected, name.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyOrWhitespace_ThrowsDomainException(string input)
    {
        Assert.Throws<DomainException>(() => new TeamName(input));
    }

    [Fact]
    public void Constructor_NullInput_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => new TeamName(null!));
    }

    [Fact]
    public void EqualityIsValueBased()
    {
        var a = new TeamName("Backend");
        var b = new TeamName("Backend");
        Assert.Equal(a, b);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var name = new TeamName("  Platform  ");
        Assert.Equal("Platform", name.ToString());
    }
}
