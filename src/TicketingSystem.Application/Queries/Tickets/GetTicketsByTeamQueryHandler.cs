using MediatR;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

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
                ToApiString(t.Type),
                ToApiString(t.State),
                t.Title,
                t.EpicId,
                t.EpicId is { } epicId && epicTitles.TryGetValue(epicId, out var title) ? title : null,
                t.CreatedAt,
                t.ModifiedAt))
            .ToList();
    }

    private static string ToApiString(TicketType type) => type switch
    {
        TicketType.Bug => "bug",
        TicketType.Feature => "feature",
        TicketType.Fix => "fix",
        _ => type.ToString().ToLowerInvariant(),
    };

    private static string ToApiString(TicketState state) => state switch
    {
        TicketState.New => "new",
        TicketState.ReadyForImplementation => "ready_for_implementation",
        TicketState.InProgress => "in_progress",
        TicketState.ReadyForAcceptance => "ready_for_acceptance",
        TicketState.Done => "done",
        _ => state.ToString().ToLowerInvariant(),
    };
}
