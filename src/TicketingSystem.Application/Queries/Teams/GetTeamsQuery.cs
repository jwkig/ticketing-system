using MediatR;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Queries.Teams;

public record GetTeamsQuery : IRequest<IReadOnlyList<TeamDto>>;
