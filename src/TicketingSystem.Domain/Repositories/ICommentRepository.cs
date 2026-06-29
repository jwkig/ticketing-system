using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Domain.Repositories;

public interface ICommentRepository
{
    Task<IReadOnlyList<Comment>> GetByTicketAsync(Guid ticketId, CancellationToken ct = default);
    Task AddAsync(Comment comment, CancellationToken ct = default);
}
