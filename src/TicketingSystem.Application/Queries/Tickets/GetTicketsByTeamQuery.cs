using MediatR;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Queries.Tickets;

public record GetTicketsByTeamQuery(Guid TeamId) : IRequest<IReadOnlyList<TicketSummaryDto>>;
