using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Domain.Entities;

public sealed class Epic
{
    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ModifiedAt { get; private set; }

    private Epic() { }

    public static Epic Create(Guid teamId, string title, string? description, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Epic title must not be empty.");

        return new()
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            Title = title.Trim(),
            Description = description,
            CreatedAt = now,
            ModifiedAt = now
        };
    }

    public void Update(string title, string? description, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Epic title must not be empty.");

        var trimmed = title.Trim();
        if (Title == trimmed && Description == description) return;

        Title = trimmed;
        Description = description;
        ModifiedAt = now;
    }
}
