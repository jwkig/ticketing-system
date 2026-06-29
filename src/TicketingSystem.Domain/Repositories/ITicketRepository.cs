using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Domain.Repositories;

public record TicketFilter(
    TicketType? Type = null,
    Guid? EpicId = null,
    string? Search = null,
    TicketState? State = null);

public interface ITicketRepository
{
    Task<IReadOnlyList<Ticket>> GetByTeamAsync(Guid teamId, TicketFilter filter, CancellationToken ct = default);
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Ticket ticket, CancellationToken ct = default);
    Task UpdateAsync(Ticket ticket, CancellationToken ct = default);
    Task DeleteAsync(Ticket ticket, CancellationToken ct = default);
}
