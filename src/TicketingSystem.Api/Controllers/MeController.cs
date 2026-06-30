using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingSystem.Application.Services;

namespace TicketingSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public sealed class MeController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;

    public MeController(ICurrentUserService currentUser) => _currentUser = currentUser;

    [HttpGet]
    public IActionResult Get() => Ok(new { userId = _currentUser.UserId });
}
