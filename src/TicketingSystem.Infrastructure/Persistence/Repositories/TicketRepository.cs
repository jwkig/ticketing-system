using Microsoft.EntityFrameworkCore;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Infrastructure.Persistence.Repositories;

public sealed class TicketRepository : ITicketRepository
{
    private readonly AppDbContext _context;

    public TicketRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<Ticket>> GetByTeamAsync(Guid teamId, TicketFilter filter, CancellationToken ct = default)
    {
        var query = _context.Tickets.Where(t => t.TeamId == teamId);

        if (filter.Type.HasValue)
            query = query.Where(t => t.Type == filter.Type.Value);
        if (filter.EpicId.HasValue)
            query = query.Where(t => t.EpicId == filter.EpicId.Value);
        if (filter.State.HasValue)
            query = query.Where(t => t.State == filter.State.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(t => EF.Functions.ILike(t.Title, $"%{filter.Search.Trim()}%"));

        // Spec: within a board column, cards are ordered most-recently-modified first.
        return await query.OrderByDescending(t => t.ModifiedAt).ToListAsync(ct);
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Tickets.FindAsync([id], ct);

    public async Task AddAsync(Ticket ticket, CancellationToken ct = default) =>
        await _context.Tickets.AddAsync(ticket, ct);

    public Task UpdateAsync(Ticket ticket, CancellationToken ct = default)
    {
        _context.Tickets.Update(ticket);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Ticket ticket, CancellationToken ct = default)
    {
        _context.Tickets.Remove(ticket);
        return Task.CompletedTask;
    }
}
