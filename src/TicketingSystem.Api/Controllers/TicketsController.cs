using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingSystem.Application.Commands.Tickets;
using TicketingSystem.Application.DTOs;
using TicketingSystem.Application.Queries.Tickets;

namespace TicketingSystem.Api.Controllers;

[ApiController]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    private readonly ISender _sender;

    public TicketsController(ISender sender) => _sender = sender;

    /// <summary>Lists a team's tickets for the Kanban board.</summary>
    [HttpGet("api/teams/{teamId:guid}/tickets")]
    public async Task<ActionResult<IReadOnlyList<TicketSummaryDto>>> GetByTeam(Guid teamId, CancellationToken ct)
        => Ok(await _sender.Send(new GetTicketsByTeamQuery(teamId), ct));

    /// <summary>Gets a single ticket with its full body.</summary>
    [HttpGet("api/tickets/{id:guid}")]
    public async Task<ActionResult<TicketDetailDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await _sender.Send(new GetTicketByIdQuery(id), ct));

    [HttpPost("api/teams/{teamId:guid}/tickets")]
    public async Task<ActionResult<TicketDetailDto>> Create(Guid teamId, [FromBody] SaveTicketRequest body, CancellationToken ct)
    {
        var ticket = await _sender.Send(
            new CreateTicketCommand(teamId, body.Type, body.Title, body.Body, body.EpicId), ct);
        return Created($"/api/tickets/{ticket.Id}", ticket);
    }

    [HttpPut("api/tickets/{id:guid}")]
    public async Task<ActionResult<TicketDetailDto>> Update(Guid id, [FromBody] SaveTicketRequest body, CancellationToken ct)
        => Ok(await _sender.Send(new UpdateTicketCommand(id, body.Type, body.Title, body.Body, body.EpicId), ct));

    [HttpPatch("api/tickets/{id:guid}/state")]
    public async Task<ActionResult<TicketDetailDto>> ChangeState(Guid id, [FromBody] ChangeTicketStateRequest body, CancellationToken ct)
        => Ok(await _sender.Send(new ChangeTicketStateCommand(id, body.State), ct));

    [HttpDelete("api/tickets/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteTicketCommand(id), ct);
        return NoContent();
    }
}

/// <summary>Request body for creating/updating a ticket. Team is fixed by the route/creation.</summary>
public record SaveTicketRequest(string Type, string Title, string Body, Guid? EpicId);

/// <summary>Request body for moving a ticket to a new workflow state.</summary>
public record ChangeTicketStateRequest(string State);
