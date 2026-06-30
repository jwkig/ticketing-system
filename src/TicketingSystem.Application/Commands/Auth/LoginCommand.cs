using MediatR;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Application.Commands.Auth;

public record LoginCommand(string Email, string Password) : IRequest<JwtTokenDto>;
