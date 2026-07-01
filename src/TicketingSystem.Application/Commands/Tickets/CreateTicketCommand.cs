using MediatR;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Commands.Tickets;

public record CreateTicketCommand(
    Guid TeamId,
    string Type,
    string Title,
    string Body,
    Guid? EpicId) : IRequest<TicketDetailDto>;
