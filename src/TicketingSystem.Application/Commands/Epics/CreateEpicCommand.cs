using MediatR;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Commands.Epics;

public record CreateEpicCommand(Guid TeamId, string Title, string? Description) : IRequest<EpicDto>;
