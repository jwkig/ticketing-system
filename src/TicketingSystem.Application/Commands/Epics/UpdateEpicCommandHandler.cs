using MediatR;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Application.Commands.Epics;

public class UpdateEpicCommandHandler : IRequestHandler<UpdateEpicCommand, EpicDto>
{
    private readonly IEpicRepository _epics;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;

    public UpdateEpicCommandHandler(IEpicRepository epics, IDateTimeProvider clock, IUnitOfWork uow)
    {
        _epics = epics;
        _clock = clock;
        _uow = uow;
    }

    public async Task<EpicDto> Handle(UpdateEpicCommand request, CancellationToken cancellationToken)
    {
        var epic = await _epics.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Epic not found.");

        // The team is fixed at creation; only title/description change here.
        epic.Update(request.Title, request.Description, _clock.UtcNow);
        await _epics.UpdateAsync(epic, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        var ticketCount = await _epics.GetTicketCountAsync(epic.Id, cancellationToken);
        return new EpicDto(
            epic.Id, epic.TeamId, epic.Title, epic.Description, epic.CreatedAt, epic.ModifiedAt, ticketCount);
    }
}
