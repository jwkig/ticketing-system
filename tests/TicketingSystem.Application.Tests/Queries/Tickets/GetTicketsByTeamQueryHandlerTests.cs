using TicketingSystem.Application.Queries.Tickets;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Tests.Queries.Tickets;

public class GetTicketsByTeamQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IEpicRepository> _epics = new();
    private readonly GetTicketsByTeamQueryHandler _sut;

    public GetTicketsByTeamQueryHandlerTests() => _sut = new GetTicketsByTeamQueryHandler(_tickets.Object, _epics.Object);

    [Fact]
    public async Task Handle_MapsTicketsToDtos_WithSnakeCaseEnums_AndEpicTitles()
    {
        var teamId = Guid.NewGuid();
        var author = Guid.NewGuid();
        var epic = Epic.Create(teamId, "Checkout", null, Now);

        var withEpic = Ticket.Create(teamId, epic.Id, teamId, author, TicketType.Bug, "Login fails", "body", Now);
        var noEpic = Ticket.Create(teamId, null, null, author, TicketType.Feature, "Dark mode", "body", Now);
        noEpic.SetState(TicketState.InProgress, Now);

        _tickets
            .Setup(x => x.GetByTeamAsync(teamId, It.IsAny<TicketFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([withEpic, noEpic]);
        _epics
            .Setup(x => x.GetByTeamAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([epic]);

        var result = await _sut.Handle(new GetTicketsByTeamQuery(teamId), default);

        Assert.Equal(2, result.Count);

        Assert.Equal("Login fails", result[0].Title);
        Assert.Equal("bug", result[0].Type);
        Assert.Equal("new", result[0].State);
        Assert.Equal(epic.Id, result[0].EpicId);
        Assert.Equal("Checkout", result[0].EpicTitle);

        Assert.Equal("Dark mode", result[1].Title);
        Assert.Equal("feature", result[1].Type);
        Assert.Equal("in_progress", result[1].State);
        Assert.Null(result[1].EpicId);
        Assert.Null(result[1].EpicTitle);
    }

    [Fact]
    public async Task Handle_EpicTitleIsNull_WhenEpicIdNotAmongTeamEpics()
    {
        var teamId = Guid.NewGuid();
        var ticket = Ticket.Create(teamId, Guid.NewGuid(), teamId, Guid.NewGuid(), TicketType.Fix, "Patch", "body", Now);

        _tickets
            .Setup(x => x.GetByTeamAsync(teamId, It.IsAny<TicketFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([ticket]);
        _epics
            .Setup(x => x.GetByTeamAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.Handle(new GetTicketsByTeamQuery(teamId), default);

        Assert.Single(result);
        Assert.Equal("fix", result[0].Type);
        Assert.NotNull(result[0].EpicId);
        Assert.Null(result[0].EpicTitle);
    }

    [Fact]
    public async Task Handle_ReturnsEmpty_WhenTeamHasNoTickets()
    {
        var teamId = Guid.NewGuid();
        _tickets
            .Setup(x => x.GetByTeamAsync(teamId, It.IsAny<TicketFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _epics
            .Setup(x => x.GetByTeamAsync(teamId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.Handle(new GetTicketsByTeamQuery(teamId), default);

        Assert.Empty(result);
    }
}
