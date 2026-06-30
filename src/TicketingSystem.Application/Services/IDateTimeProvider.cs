namespace TicketingSystem.Application.Services;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
