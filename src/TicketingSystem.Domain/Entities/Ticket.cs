using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Domain.Entities;

public sealed class Ticket
{
    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid? EpicId { get; private set; }
    public Guid CreatedById { get; private set; }
    public TicketType Type { get; private set; }
    public TicketState State { get; private set; }
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ModifiedAt { get; private set; }

    private Ticket() { }

    public static Ticket Create(
        Guid teamId,
        Guid? epicId,
        Guid? epicTeamId,
        Guid createdById,
        TicketType type,
        string title,
        string body,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Ticket title must not be empty.");

        if (string.IsNullOrWhiteSpace(body))
            throw new DomainException("Ticket body must not be empty.");

        if (epicId.HasValue && epicTeamId != teamId)
            throw new DomainException("Epic does not belong to the same team as the ticket.");

        return new()
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            EpicId = epicId,
            CreatedById = createdById,
            Type = type,
            State = TicketState.New,
            Title = title.Trim(),
            Body = body,
            CreatedAt = now,
            ModifiedAt = now
        };
    }

    public void Update(
        Guid teamId,
        Guid? epicId,
        Guid? epicTeamId,
        TicketType type,
        string title,
        string body,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Ticket title must not be empty.");

        if (string.IsNullOrWhiteSpace(body))
            throw new DomainException("Ticket body must not be empty.");

        if (epicId.HasValue && epicTeamId != teamId)
            throw new DomainException("Epic does not belong to the same team as the ticket.");

        var trimmedTitle = title.Trim();
        if (TeamId == teamId && EpicId == epicId && Type == type && Title == trimmedTitle && Body == body)
            return;

        TeamId = teamId;
        EpicId = epicId;
        Type = type;
        Title = trimmedTitle;
        Body = body;
        ModifiedAt = now;
    }

    public void SetState(TicketState newState, DateTimeOffset now)
    {
        if (State == newState) return;
        State = newState;
        ModifiedAt = now;
    }
}
