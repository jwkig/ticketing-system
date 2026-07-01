using TicketingSystem.Application.DTOs;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.Application.Common;

public static class TicketMapping
{
    public static TicketDetailDto ToDetailDto(this Ticket ticket, string? epicTitle) =>
        new(
            ticket.Id,
            ticket.TeamId,
            TicketEnumMap.ToApiString(ticket.Type),
            TicketEnumMap.ToApiString(ticket.State),
            ticket.Title,
            ticket.Body,
            ticket.EpicId,
            epicTitle,
            ticket.CreatedById,
            ticket.CreatedAt,
            ticket.ModifiedAt);
}
