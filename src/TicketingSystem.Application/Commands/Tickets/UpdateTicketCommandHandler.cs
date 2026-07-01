using MediatR;
using TicketingSystem.Application.Common;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Application.Commands.Tickets;

public class UpdateTicketCommandHandler : IRequestHandler<UpdateTicketCommand, TicketDetailDto>
{
    private readonly ITicketRepository _tickets;
    private readonly IEpicRepository _epics;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;

    public UpdateTicketCommandHandler(
        ITicketRepository tickets,
        IEpicRepository epics,
        IDateTimeProvider clock,
        IUnitOfWork uow)
    {
        _tickets = tickets;
        _epics = epics;
        _clock = clock;
        _uow = uow;
    }

    public async Task<TicketDetailDto> Handle(UpdateTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Ticket not found.");

        Epic? epic = null;
        if (request.EpicId is { } epicId)
        {
            epic = await _epics.GetByIdAsync(epicId, cancellationToken)
                ?? throw new NotFoundException("Epic not found.");
        }

        // The team is fixed at creation; only type/title/body/epic change here.
        ticket.Update(
            ticket.TeamId,
            epic?.Id,
            epic?.TeamId,
            TicketEnumMap.ParseType(request.Type),
            request.Title,
            request.Body,
            _clock.UtcNow);

        await _tickets.UpdateAsync(ticket, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return ticket.ToDetailDto(epic?.Title);
    }
}
