using MediatR;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Application.Commands.Teams;

public class DeleteTeamCommandHandler : IRequestHandler<DeleteTeamCommand>
{
    private readonly ITeamRepository _teams;
    private readonly IUnitOfWork _uow;

    public DeleteTeamCommandHandler(ITeamRepository teams, IUnitOfWork uow)
    {
        _teams = teams;
        _uow = uow;
    }

    public async Task Handle(DeleteTeamCommand request, CancellationToken cancellationToken)
    {
        var team = await _teams.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Team not found.");

        if (await _teams.HasTicketsOrEpicsAsync(team.Id, cancellationToken))
            throw new ConflictException("Cannot delete a team that has tickets or epics.");

        await _teams.DeleteAsync(team, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
