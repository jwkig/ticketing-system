using MediatR;

namespace TicketingSystem.Application.Commands.Tickets;

public record DeleteTicketCommand(Guid Id) : IRequest;
