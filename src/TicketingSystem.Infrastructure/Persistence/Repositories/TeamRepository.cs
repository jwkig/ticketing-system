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

    public async Task<bool> ExistsByNameAsync(string normalisedName, Guid? excludeId = null, CancellationToken ct = default)
    {
        // Load names into memory for case-insensitive comparison.
        // Acceptable for typical team counts in this application.
        var names = await _context.Teams
            .Where(t => excludeId == null || t.Id != excludeId)
            .Select(t => EF.Property<string>(t, "Name"))
            .ToListAsync(ct);

        return names.Any(n => string.Equals(n, normalisedName, StringComparison.OrdinalIgnoreCase));
    }
}
