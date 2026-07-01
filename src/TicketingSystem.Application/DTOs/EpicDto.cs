namespace TicketingSystem.Application.DTOs;

public record EpicDto(
    Guid Id,
    Guid TeamId,
    string Title,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    int TicketCount);
