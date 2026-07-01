using MediatR;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Commands.Epics;

public record UpdateEpicCommand(Guid Id, string Title, string? Description) : IRequest<EpicDto>;
