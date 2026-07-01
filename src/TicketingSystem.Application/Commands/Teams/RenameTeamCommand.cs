using MediatR;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Commands.Teams;

public record RenameTeamCommand(Guid Id, string Name) : IRequest<TeamDto>;
