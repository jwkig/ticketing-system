using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Domain.Entities;

public sealed class EmailVerificationToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }

    private EmailVerificationToken() { }

    public static EmailVerificationToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            IsUsed = false
        };

    public void Use(DateTimeOffset now)
    {
        if (IsUsed)
            throw new DomainException("Verification token has already been used.");

        if (now >= ExpiresAt)
            throw new DomainException("Verification token has expired.");

        IsUsed = true;
    }
}
