using MediatR;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Application.Queries.Epics;

public class GetEpicsByTeamQueryHandler : IRequestHandler<GetEpicsByTeamQuery, IReadOnlyList<EpicDto>>
{
    private readonly IEpicRepository _epics;

    public GetEpicsByTeamQueryHandler(IEpicRepository epics) => _epics = epics;

    public async Task<IReadOnlyList<EpicDto>> Handle(GetEpicsByTeamQuery request, CancellationToken cancellationToken)
    {
        var epics = await _epics.GetByTeamAsync(request.TeamId, cancellationToken);
        var dtos = new List<EpicDto>(epics.Count);

        foreach (var epic in epics)
        {
            var ticketCount = await _epics.GetTicketCountAsync(epic.Id, cancellationToken);
            dtos.Add(new EpicDto(
                epic.Id, epic.TeamId, epic.Title, epic.Description, epic.CreatedAt, epic.ModifiedAt, ticketCount));
        }

        return dtos;
    }
}
