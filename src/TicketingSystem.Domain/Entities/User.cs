using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public EmailAddress Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public bool IsEmailVerified { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private User() { }

    public static User Create(EmailAddress email, string passwordHash, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            IsEmailVerified = false,
            CreatedAt = now
        };

    public void VerifyEmail() => IsEmailVerified = true;
}
