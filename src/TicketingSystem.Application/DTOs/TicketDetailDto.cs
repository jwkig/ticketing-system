namespace TicketingSystem.Application.DTOs;

/// <summary>
/// A single ticket with its full body, returned by create/update/state/get-by-id.
/// <c>Type</c> and <c>State</c> are the canonical API strings (e.g. "bug", "in_progress").
/// </summary>
public record TicketDetailDto(
    Guid Id,
    Guid TeamId,
    string Type,
    string State,
    string Title,
    string Body,
    Guid? EpicId,
    string? EpicTitle,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);
