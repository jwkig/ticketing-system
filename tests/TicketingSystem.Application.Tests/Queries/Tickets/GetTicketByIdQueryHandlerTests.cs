using TicketingSystem.Application.Queries.Tickets;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Tests.Queries.Tickets;

public class GetTicketByIdQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IEpicRepository> _epics = new();
    private readonly GetTicketByIdQueryHandler _sut;

    public GetTicketByIdQueryHandlerTests() => _sut = new GetTicketByIdQueryHandler(_tickets.Object, _epics.Object);

    [Fact]
    public async Task Handle_UnknownTicket_ThrowsNotFound()
    {
        _tickets.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.Handle(new GetTicketByIdQuery(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_ReturnsDetailWithBodyAndEpicTitle()
    {
        var teamId = Guid.NewGuid();
        var epic = Epic.Create(teamId, "Checkout", null, Now);
        var ticket = Ticket.Create(teamId, epic.Id, teamId, Guid.NewGuid(), TicketType.Bug, "Login fails", "repro steps", Now);
        _tickets.Setup(x => x.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        _epics.Setup(x => x.GetByIdAsync(epic.Id, It.IsAny<CancellationToken>())).ReturnsAsync(epic);

        var result = await _sut.Handle(new GetTicketByIdQuery(ticket.Id), default);

        Assert.Equal("bug", result.Type);
        Assert.Equal("new", result.State);
        Assert.Equal("repro steps", result.Body);
        Assert.Equal(epic.Id, result.EpicId);
        Assert.Equal("Checkout", result.EpicTitle);
    }

    [Fact]
    public async Task Handle_TicketWithoutEpic_ReturnsNullEpicTitle()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), null, null, Guid.NewGuid(), TicketType.Feature, "T", "B", Now);
        _tickets.Setup(x => x.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        var result = await _sut.Handle(new GetTicketByIdQuery(ticket.Id), default);

        Assert.Null(result.EpicId);
        Assert.Null(result.EpicTitle);
    }
}
