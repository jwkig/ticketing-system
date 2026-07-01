namespace TicketingSystem.Application.DTOs;

/// <summary>
/// A ticket as shown on the Kanban board. <c>Type</c> and <c>State</c> are the
/// canonical API strings (e.g. "bug", "ready_for_implementation").
/// </summary>
public record TicketSummaryDto(
    Guid Id,
    Guid TeamId,
    string Type,
    string State,
    string Title,
    Guid? EpicId,
    string? EpicTitle,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);
