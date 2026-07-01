using MediatR;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Commands.Teams;

public class CreateTeamCommandHandler : IRequestHandler<CreateTeamCommand, TeamDto>
{
    private readonly ITeamRepository _teams;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;

    public CreateTeamCommandHandler(ITeamRepository teams, IDateTimeProvider clock, IUnitOfWork uow)
    {
        _teams = teams;
        _clock = clock;
        _uow = uow;
    }

    public async Task<TeamDto> Handle(CreateTeamCommand request, CancellationToken cancellationToken)
    {
        var name = new TeamName(request.Name);

        if (await _teams.ExistsByNameAsync(name.Value, null, cancellationToken))
            throw new ConflictException("A team with this name already exists.");

        var team = Team.Create(name, _clock.UtcNow);
        await _teams.AddAsync(team, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new TeamDto(team.Id, team.Name.Value, team.CreatedAt, team.ModifiedAt, 0, 0);
    }
}
