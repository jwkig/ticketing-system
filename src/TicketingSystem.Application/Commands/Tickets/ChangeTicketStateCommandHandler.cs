using MediatR;
using TicketingSystem.Application.Common;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Application.Commands.Tickets;

public class ChangeTicketStateCommandHandler : IRequestHandler<ChangeTicketStateCommand, TicketDetailDto>
{
    private readonly ITicketRepository _tickets;
    private readonly IEpicRepository _epics;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;

    public ChangeTicketStateCommandHandler(
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

    public async Task<TicketDetailDto> Handle(ChangeTicketStateCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Ticket not found.");

        ticket.SetState(TicketEnumMap.ParseState(request.State), _clock.UtcNow);

        await _tickets.UpdateAsync(ticket, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        string? epicTitle = null;
        if (ticket.EpicId is { } epicId)
        {
            var epic = await _epics.GetByIdAsync(epicId, cancellationToken);
            epicTitle = epic?.Title;
        }

        return ticket.ToDetailDto(epicTitle);
    }
}
