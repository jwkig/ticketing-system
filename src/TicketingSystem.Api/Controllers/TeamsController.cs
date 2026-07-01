using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingSystem.Application.Commands.Teams;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Queries.Teams;

namespace TicketingSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/teams")]
public sealed class TeamsController : ControllerBase
{
    private readonly ISender _sender;

    public TeamsController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TeamDto>>> GetAll(CancellationToken ct)
        => Ok(await _sender.Send(new GetTeamsQuery(), ct));

    [HttpPost]
    public async Task<ActionResult<TeamDto>> Create([FromBody] CreateTeamCommand command, CancellationToken ct)
    {
        var team = await _sender.Send(command, ct);
        return Created($"/api/teams/{team.Id}", team);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TeamDto>> Rename(Guid id, [FromBody] TeamNameRequest body, CancellationToken ct)
        => Ok(await _sender.Send(new RenameTeamCommand(id, body.Name), ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteTeamCommand(id), ct);
        return NoContent();
    }
}

/// <summary>Request body for creating/renaming a team.</summary>
public record TeamNameRequest(string Name);
