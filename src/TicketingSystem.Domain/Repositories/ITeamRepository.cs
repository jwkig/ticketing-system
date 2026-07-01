using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Domain.Repositories;

/// <summary>Number of tickets and epics that reference a team.</summary>
public sealed record TeamReferenceCounts(int Tickets, int Epics);

public interface ITeamRepository
{
    Task<IReadOnlyList<Team>> GetAllAsync(CancellationToken ct = default);
    Task<Team?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Team team, CancellationToken ct = default);
    Task UpdateAsync(Team team, CancellationToken ct = default);
    Task DeleteAsync(Team team, CancellationToken ct = default);
    Task<bool> HasTicketsOrEpicsAsync(Guid teamId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string normalisedName, Guid? excludeId = null, CancellationToken ct = default);
    Task<TeamReferenceCounts> GetReferenceCountsAsync(Guid teamId, CancellationToken ct = default);
}
