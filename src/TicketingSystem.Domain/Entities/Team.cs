using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Domain.Entities;

public sealed class Team
{
    public Guid Id { get; private set; }
    public TeamName Name { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ModifiedAt { get; private set; }

    private Team() { }

    public static Team Create(TeamName name, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = now,
            ModifiedAt = now
        };

    public void Rename(TeamName newName, DateTimeOffset now)
    {
        if (Name == newName) return;
        Name = newName;
        ModifiedAt = now;
    }
}
