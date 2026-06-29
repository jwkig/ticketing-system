using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Domain.Entities;

public sealed class Comment
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Body { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }

    private Comment() { }

    public static Comment Create(Guid ticketId, Guid authorId, string body, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new DomainException("Comment body must not be empty.");

        return new()
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorId = authorId,
            Body = body,
            CreatedAt = now
        };
    }
}
