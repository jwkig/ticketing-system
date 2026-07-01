using MediatR;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Commands.Tickets;

public record ChangeTicketStateCommand(Guid Id, string State) : IRequest<TicketDetailDto>;
