using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Domain.Repositories;

public interface IVerificationTokenRepository
{
    Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(EmailVerificationToken token, CancellationToken ct = default);
    Task InvalidatePreviousForUserAsync(Guid userId, CancellationToken ct = default);
}
