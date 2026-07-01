using MediatR;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Application.Commands.Epics;

public class CreateEpicCommandHandler : IRequestHandler<CreateEpicCommand, EpicDto>
{
    private readonly IEpicRepository _epics;
    private readonly ITeamRepository _teams;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;

    public CreateEpicCommandHandler(
        IEpicRepository epics,
        ITeamRepository teams,
        IDateTimeProvider clock,
        IUnitOfWork uow)
    {
        _epics = epics;
        _teams = teams;
        _clock = clock;
        _uow = uow;
    }

    public async Task<EpicDto> Handle(CreateEpicCommand request, CancellationToken cancellationToken)
    {
        // The epic's team is fixed at creation and must reference an existing team.
        var team = await _teams.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new NotFoundException("Team not found.");

        var epic = Epic.Create(team.Id, request.Title, request.Description, _clock.UtcNow);
        await _epics.AddAsync(epic, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return new EpicDto(
            epic.Id, epic.TeamId, epic.Title, epic.Description, epic.CreatedAt, epic.ModifiedAt, 0);
    }
}
