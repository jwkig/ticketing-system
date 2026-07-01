using MediatR;

namespace TicketingSystem.Application.Commands.Teams;

public record DeleteTeamCommand(Guid Id) : IRequest;
