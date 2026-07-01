namespace TicketingSystem.Application.DTOs;

public record TeamDto(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    int TicketCount,
    int EpicCount);
