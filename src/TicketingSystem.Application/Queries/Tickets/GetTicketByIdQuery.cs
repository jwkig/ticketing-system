using MediatR;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Queries.Tickets;

public record GetTicketByIdQuery(Guid Id) : IRequest<TicketDetailDto>;
