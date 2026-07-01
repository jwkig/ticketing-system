using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingSystem.Application.Commands.Epics;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Queries.Epics;

namespace TicketingSystem.Api.Controllers;

[ApiController]
[Authorize]
public sealed class EpicsController : ControllerBase
{
    private readonly ISender _sender;

    public EpicsController(ISender sender) => _sender = sender;

    [HttpGet("api/teams/{teamId:guid}/epics")]
    public async Task<ActionResult<IReadOnlyList<EpicDto>>> GetByTeam(Guid teamId, CancellationToken ct)
        => Ok(await _sender.Send(new GetEpicsByTeamQuery(teamId), ct));

    [HttpPost("api/teams/{teamId:guid}/epics")]
    public async Task<ActionResult<EpicDto>> Create(Guid teamId, [FromBody] SaveEpicRequest body, CancellationToken ct)
    {
        var epic = await _sender.Send(new CreateEpicCommand(teamId, body.Title, body.Description), ct);
        return Created($"/api/epics/{epic.Id}", epic);
    }

    [HttpPut("api/epics/{id:guid}")]
    public async Task<ActionResult<EpicDto>> Update(Guid id, [FromBody] SaveEpicRequest body, CancellationToken ct)
        => Ok(await _sender.Send(new UpdateEpicCommand(id, body.Title, body.Description), ct));

    [HttpDelete("api/epics/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteEpicCommand(id), ct);
        return NoContent();
    }
}

/// <summary>Request body for creating/updating an epic (team is fixed by the route/creation).</summary>
public record SaveEpicRequest(string Title, string? Description);
