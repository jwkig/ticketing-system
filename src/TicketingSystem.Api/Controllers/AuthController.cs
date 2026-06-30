using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingSystem.Application.Commands.Auth;
using TicketingSystem.Application.DTOs;

namespace TicketingSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

    [HttpPost("signup")]
    public async Task<IActionResult> SignUp([FromBody] SignUpCommand command, CancellationToken ct)
    {
        await _sender.Send(command, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    public async Task<ActionResult<JwtTokenDto>> Login([FromBody] LoginCommand command, CancellationToken ct)
        => Ok(await _sender.Send(command, ct));

    [HttpPost("logout")]
    public IActionResult Logout() => NoContent();

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token, CancellationToken ct)
    {
        await _sender.Send(new VerifyEmailCommand(token), ct);
        return NoContent();
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ResendVerificationCommand command, CancellationToken ct)
    {
        await _sender.Send(command, ct);
        return NoContent();
    }
}
