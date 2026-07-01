using MediatR;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Application.Commands.Tickets;

public class DeleteTicketCommandHandler : IRequestHandler<DeleteTicketCommand>
{
    private readonly ITicketRepository _tickets;
    private readonly IUnitOfWork _uow;

    public DeleteTicketCommandHandler(ITicketRepository tickets, IUnitOfWork uow)
    {
        _tickets = tickets;
        _uow = uow;
    }

    public async Task Handle(DeleteTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _tickets.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Ticket not found.");

        await _tickets.DeleteAsync(ticket, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
