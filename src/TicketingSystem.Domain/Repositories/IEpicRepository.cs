using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Domain.Repositories;

public interface IEpicRepository
{
    Task<IReadOnlyList<Epic>> GetByTeamAsync(Guid teamId, CancellationToken ct = default);
    Task<Epic?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Epic epic, CancellationToken ct = default);
    Task UpdateAsync(Epic epic, CancellationToken ct = default);
    Task DeleteAsync(Epic epic, CancellationToken ct = default);
    Task<bool> HasTicketsAsync(Guid epicId, CancellationToken ct = default);
    Task<int> GetTicketCountAsync(Guid epicId, CancellationToken ct = default);
}
