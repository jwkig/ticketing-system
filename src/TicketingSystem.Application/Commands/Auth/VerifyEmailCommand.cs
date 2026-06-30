using MediatR;

namespace TicketingSystem.Application.Commands.Auth;

public record VerifyEmailCommand(string Token) : IRequest;
