using MediatR;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Commands.Teams;

public class RenameTeamCommandHandler : IRequestHandler<RenameTeamCommand, TeamDto>
{
    private readonly ITeamRepository _teams;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;

    public RenameTeamCommandHandler(ITeamRepository teams, IDateTimeProvider clock, IUnitOfWork uow)
    {
        _teams = teams;
        _clock = clock;
        _uow = uow;
    }

    public async Task<TeamDto> Handle(RenameTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await _teams.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Team not found.");

        var name = new TeamName(request.Name);
        if (await _teams.ExistsByNameAsync(name.Value, request.Id, cancellationToken))
            throw new ConflictException("A team with this name already exists.");

        team.Rename(name, _clock.UtcNow);
        await _teams.UpdateAsync(team, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var counts = await _teams.GetReferenceCountsAsync(team.Id, cancellationToken);
        return new TeamDto(team.Id, team.Name.Value, team.CreatedAt, team.ModifiedAt, counts.Tickets, counts.Epics);
    }
}
