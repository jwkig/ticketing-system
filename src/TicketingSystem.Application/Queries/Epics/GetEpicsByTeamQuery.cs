using MediatR;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Queries.Epics;

public record GetEpicsByTeamQuery(Guid TeamId) : IRequest<IReadOnlyList<EpicDto>>;
