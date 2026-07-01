using MediatR;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Application.Commands.Epics;

public class DeleteEpicCommandHandler : IRequestHandler<DeleteEpicCommand>
{
    private readonly IEpicRepository _epics;
    private readonly IUnitOfWork _uow;

    public DeleteEpicCommandHandler(IEpicRepository epics, IUnitOfWork uow)
    {
        _epics = epics;
        _uow = uow;
    }

    public async Task Handle(DeleteEpicCommand request, CancellationToken cancellationToken)
    {
        var epic = await _epics.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Epic not found.");

        if (await _epics.HasTicketsAsync(epic.Id, cancellationToken))
            throw new ConflictException("Cannot delete an epic that is referenced by tickets.");

        await _epics.DeleteAsync(epic, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
