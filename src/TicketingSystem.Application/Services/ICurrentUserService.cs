namespace TicketingSystem.Application.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }
}
