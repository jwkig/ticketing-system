using MediatR;
using TicketingSystem.Application.Common;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Application.Queries.Tickets;

public class GetTicketByIdQueryHandler : IRequestHandler<GetTicketByIdQuery, TicketDetailDto>
{
    private readonly ITicketRepository _tickets;
    private readonly IEpicRepository _epics;

    public GetTicketByIdQueryHandler(ITicketRepository tickets, IEpicRepository epics)
    {
        _tickets = tickets;
        _epics = epics;
    }

    public async Task<TicketDetailDto> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Ticket not found.");

        string? epicTitle = null;
        if (ticket.EpicId is { } epicId)
        {
            var epic = await _epics.GetByIdAsync(epicId, cancellationToken);
            epicTitle = epic?.Title;
        }

        return ticket.ToDetailDto(epicTitle);
    }
}
