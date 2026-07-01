using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Queries.Tickets;

namespace TicketingSystem.Api.Controllers;

[ApiController]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    private readonly ISender _sender;

    public TicketsController(ISender sender) => _sender = sender;

    /// <summary>Lists a team's tickets for the Kanban board (read-only).</summary>
    [HttpGet("api/teams/{teamId:guid}/tickets")]
    public async Task<ActionResult<IReadOnlyList<TicketSummaryDto>>> GetByTeam(Guid teamId, CancellationToken ct)
        => Ok(await _sender.Send(new GetTicketsByTeamQuery(teamId), ct));
}
