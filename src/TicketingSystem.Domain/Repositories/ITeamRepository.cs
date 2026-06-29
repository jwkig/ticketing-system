using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Domain.Repositories;

public interface ITeamRepository
{
    Task<IReadOnlyList<Team>> GetAllAsync(CancellationToken ct = default);
    Task<Team?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Team team, CancellationToken ct = default);
    Task UpdateAsync(Team team, CancellationToken ct = default);
    Task DeleteAsync(Team team, CancellationToken ct = default);
    Task<bool> HasTicketsOrEpicsAsync(Guid teamId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string normalisedName, Guid? excludeId = null, CancellationToken ct = default);
}
