using MediatR;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Commands.Teams;

public record CreateTeamCommand(string Name) : IRequest<TeamDto>;
