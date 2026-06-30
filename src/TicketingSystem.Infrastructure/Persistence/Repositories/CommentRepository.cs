using Microsoft.EntityFrameworkCore;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Infrastructure.Persistence.Repositories;

public sealed class CommentRepository : ICommentRepository
{
    private readonly AppDbContext _context;

    public CommentRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<Comment>> GetByTicketAsync(Guid ticketId, CancellationToken ct = default) =>
        await _context.Comments
            .Where(c => c.TicketId == ticketId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Comment comment, CancellationToken ct = default) =>
        await _context.Comments.AddAsync(comment, ct);
}
