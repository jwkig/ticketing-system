using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TicketingSystem.Infrastructure.Options;
using TicketingSystem.Infrastructure.Services;

namespace TicketingSystem.Infrastructure.Tests.Services;

public class JwtServiceTests
{
    // Must be ≥ 32 bytes (256 bits) for HS256.
    private const string TestSecretKey = "test-secret-key-that-is-at-least-32-bytes-long!";

    private static JwtService CreateSut(int expirationMinutes = 60) =>
        new(Microsoft.Extensions.Options.Options.Create(new JwtSettings
        {
            SecretKey = TestSecretKey,
            ExpirationMinutes = expirationMinutes
        }));

    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        var token = CreateSut().GenerateToken(Guid.NewGuid(), "user@example.com");
        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateToken_ContainsSubjectClaim()
    {
        var userId = Guid.NewGuid();
        var token = CreateSut().GenerateToken(userId, "user@example.com");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(userId.ToString(), jwt.Subject);
    }

    [Fact]
    public void GenerateToken_ContainsEmailClaim()
    {
        const string email = "user@example.com";
        var token = CreateSut().GenerateToken(Guid.NewGuid(), email);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        Assert.Equal(email, emailClaim?.Value);
    }

    [Fact]
    public void GenerateToken_ExpiresAfterConfiguredMinutes()
    {
        var token = CreateSut(expirationMinutes: 120).GenerateToken(Guid.NewGuid(), "user@example.com");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var expectedExpiry = DateTime.UtcNow.AddMinutes(120);
        Assert.True(Math.Abs((jwt.ValidTo - expectedExpiry).TotalSeconds) < 5);
    }

    [Fact]
    public void GenerateToken_IsValidatableWithCorrectKey()
    {
        var token = CreateSut().GenerateToken(Guid.NewGuid(), "user@example.com");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecretKey));
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
        Assert.NotNull(principal);
    }

    [Fact]
    public void GenerateToken_FailsValidationWithWrongKey()
    {
        var token = CreateSut().GenerateToken(Guid.NewGuid(), "user@example.com");

        var wrongKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("wrong-key-that-is-also-at-least-32-bytes!!"));
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            IssuerSigningKey = wrongKey
        };

        Assert.ThrowsAny<SecurityTokenException>(
            () => new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _));
    }

    [Fact]
    public void GenerateToken_DifferentUsersProduceDifferentTokens()
    {
        var sut = CreateSut();
        var token1 = sut.GenerateToken(Guid.NewGuid(), "a@example.com");
        var token2 = sut.GenerateToken(Guid.NewGuid(), "b@example.com");
        Assert.NotEqual(token1, token2);
    }
}
