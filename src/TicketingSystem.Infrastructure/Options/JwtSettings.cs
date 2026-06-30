namespace TicketingSystem.Infrastructure.Options;

public sealed class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 60;
}
