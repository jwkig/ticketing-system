using Microsoft.EntityFrameworkCore;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Infrastructure.Persistence.Repositories;

public sealed class TeamRepository : ITeamRepository
{
    private readonly AppDbContext _context;

    public TeamRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<Team>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Teams.OrderBy(t => t.Name).ToListAsync(ct);

    public async Task<Team?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Teams.FindAsync([id], ct);

    public async Task AddAsync(Team team, CancellationToken ct = default) =>
        await _context.Teams.AddAsync(team, ct);

    public Task UpdateAsync(Team team, CancellationToken ct = default)
    {
        _context.Teams.Update(team);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Team team, CancellationToken ct = default)
    {
        _context.Teams.Remove(team);
        return Task.CompletedTask;
    }

    public Task<bool> HasTicketsOrEpicsAsync(Guid teamId, CancellationToken ct = default) =>
        _context.Tickets.AnyAsync(t => t.TeamId == teamId, ct)
            .ContinueWith(
                async hasTickets => hasTickets.Result || await _context.Epics.AnyAsync(e => e.TeamId == teamId, ct),
                ct, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)
            .Unwrap();

    public async Task<TeamReferenceCounts> GetReferenceCountsAsync(Guid teamId, CancellationToken ct = default)
    {
        var tickets = await _context.Tickets.CountAsync(t => t.TeamId == teamId, ct);
        var epics = await _context.Epics.CountAsync(e => e.TeamId == teamId, ct);
        return new TeamReferenceCounts(tickets, epics);
    }

    public async Task<bool> ExistsByNameAsync(string normalisedName, Guid? excludeId = null, CancellationToken ct = default)
    {
        // Materialise the teams and compare on the value object in memory (the Name
        // column is stored via a value converter, so it can't be projected directly).
        // Acceptable for typical team counts in this application.
        var teams = await _context.Teams
            .Where(t => excludeId == null || t.Id != excludeId)
            .ToListAsync(ct);

        return teams.Any(t => string.Equals(t.Name.Value, normalisedName, StringComparison.OrdinalIgnoreCase));
    }
}
