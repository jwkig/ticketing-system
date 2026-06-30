using Microsoft.EntityFrameworkCore;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Infrastructure.Persistence.Repositories;

public sealed class VerificationTokenRepository : IVerificationTokenRepository
{
    private readonly AppDbContext _context;

    public VerificationTokenRepository(AppDbContext context) => _context = context;

    public Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        _context.VerificationTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task AddAsync(EmailVerificationToken token, CancellationToken ct = default) =>
        await _context.VerificationTokens.AddAsync(token, ct);

    // ExecuteUpdateAsync runs immediately without SaveChanges; this is intentional
    // so existing tokens are invalidated before the new one is persisted.
    public Task InvalidatePreviousForUserAsync(Guid userId, CancellationToken ct = default) =>
        _context.VerificationTokens
            .Where(t => t.UserId == userId && !t.IsUsed)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsUsed, true), ct);
}
