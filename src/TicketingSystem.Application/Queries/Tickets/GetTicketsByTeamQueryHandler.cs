using MediatR;
using TicketingSystem.Application.Common;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Application.Queries.Tickets;

public class GetTicketsByTeamQueryHandler
    : IRequestHandler<GetTicketsByTeamQuery, IReadOnlyList<TicketSummaryDto>>
{
    private readonly ITicketRepository _tickets;
    private readonly IEpicRepository _epics;

    public GetTicketsByTeamQueryHandler(ITicketRepository tickets, IEpicRepository epics)
    {
        _tickets = tickets;
        _epics = epics;
    }

    public async Task<IReadOnlyList<TicketSummaryDto>> Handle(
        GetTicketsByTeamQuery request, CancellationToken cancellationToken)
    {
        var tickets = await _tickets.GetByTeamAsync(request.TeamId, new TicketFilter(), cancellationToken);
        var epics = await _epics.GetByTeamAsync(request.TeamId, cancellationToken);
        var epicTitles = epics.ToDictionary(e => e.Id, e => e.Title);

        return tickets
            .Select(t => new TicketSummaryDto(
                t.Id,
                t.TeamId,
                TicketEnumMap.ToApiString(t.Type),
                TicketEnumMap.ToApiString(t.State),
                t.Title,
                t.EpicId,
                t.EpicId is { } epicId && epicTitles.TryGetValue(epicId, out var title) ? title : null,
                t.CreatedAt,
                t.ModifiedAt))
            .ToList();
    }
}
