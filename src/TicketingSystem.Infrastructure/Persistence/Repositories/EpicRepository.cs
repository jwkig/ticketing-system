using Microsoft.EntityFrameworkCore;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Infrastructure.Persistence.Repositories;

public sealed class EpicRepository : IEpicRepository
{
    private readonly AppDbContext _context;

    public EpicRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<Epic>> GetByTeamAsync(Guid teamId, CancellationToken ct = default) =>
        await _context.Epics
            .Where(e => e.TeamId == teamId)
            .OrderBy(e => e.Title)
            .ToListAsync(ct);

    public async Task<Epic?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Epics.FindAsync([id], ct);

    public async Task AddAsync(Epic epic, CancellationToken ct = default) =>
        await _context.Epics.AddAsync(epic, ct);

    public Task UpdateAsync(Epic epic, CancellationToken ct = default)
    {
        _context.Epics.Update(epic);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Epic epic, CancellationToken ct = default)
    {
        _context.Epics.Remove(epic);
        return Task.CompletedTask;
    }

    public Task<bool> HasTicketsAsync(Guid epicId, CancellationToken ct = default) =>
        _context.Tickets.AnyAsync(t => t.EpicId == epicId, ct);
}
