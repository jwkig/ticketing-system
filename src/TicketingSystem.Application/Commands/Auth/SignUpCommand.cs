using MediatR;

namespace TicketingSystem.Application.Commands.Auth;

public record SignUpCommand(string Email, string Password) : IRequest;
