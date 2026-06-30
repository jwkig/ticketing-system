using MediatR;

namespace TicketingSystem.Application.Commands.Auth;

public record ResendVerificationCommand(string Email) : IRequest;
