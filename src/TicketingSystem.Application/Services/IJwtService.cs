namespace TicketingSystem.Application.Services;

public interface IJwtService
{
    string GenerateToken(Guid userId, string email);
}
