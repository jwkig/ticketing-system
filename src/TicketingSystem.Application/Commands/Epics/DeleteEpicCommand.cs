using MediatR;

namespace TicketingSystem.Application.Commands.Epics;

public record DeleteEpicCommand(Guid Id) : IRequest;
