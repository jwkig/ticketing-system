using MediatR;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Application.Queries.Teams;

public class GetTeamsQueryHandler : IRequestHandler<GetTeamsQuery, IReadOnlyList<TeamDto>>
{
    private readonly ITeamRepository _teams;

    public GetTeamsQueryHandler(ITeamRepository teams) => _teams = teams;

    public async Task<IReadOnlyList<TeamDto>> Handle(GetTeamsQuery request, CancellationToken cancellationToken)
    {
        var teams = await _teams.GetAllAsync(cancellationToken);
        var dtos = new List<TeamDto>(teams.Count);

        foreach (var team in teams)
        {
            var counts = await _teams.GetReferenceCountsAsync(team.Id, cancellationToken);
            dtos.Add(new TeamDto(
                team.Id, team.Name.Value, team.CreatedAt, team.ModifiedAt, counts.Tickets, counts.Epics));
        }

        return dtos;
    }
}
