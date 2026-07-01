using MediatR;
using TicketingSystem.Application.Common;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Application.Commands.Tickets;

public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, TicketDetailDto>
{
    private readonly ITicketRepository _tickets;
    private readonly IEpicRepository _epics;
    private readonly ITeamRepository _teams;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;

    public CreateTicketCommandHandler(
        ITicketRepository tickets,
        IEpicRepository epics,
        ITeamRepository teams,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IUnitOfWork uow)
    {
        _tickets = tickets;
        _epics = epics;
        _teams = teams;
        _currentUser = currentUser;
        _clock = clock;
        _uow = uow;
    }

    public async Task<TicketDetailDto> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        var team = await _teams.GetByIdAsync(request.TeamId, cancellationToken)
            ?? throw new NotFoundException("Team not found.");

        Epic? epic = null;
        if (request.EpicId is { } epicId)
        {
            epic = await _epics.GetByIdAsync(epicId, cancellationToken)
                ?? throw new NotFoundException("Epic not found.");
        }

        var ticket = Ticket.Create(
            team.Id,
            epic?.Id,
            epic?.TeamId,
            _currentUser.UserId,
            TicketEnumMap.ParseType(request.Type),
            request.Title,
            request.Body,
            _clock.UtcNow);

        await _tickets.AddAsync(ticket, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ticket.ToDetailDto(epic?.Title);
    }
}
