using TicketingSystem.Application.Queries.Epics;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Application.Tests.Queries.Epics;

public class GetEpicsByTeamQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IEpicRepository> _epics = new();
    private readonly GetEpicsByTeamQueryHandler _sut;

    public GetEpicsByTeamQueryHandlerTests() => _sut = new GetEpicsByTeamQueryHandler(_epics.Object);

    [Fact]
    public async Task Handle_MapsEpicsWithTicketCounts()
    {
        var teamId = Guid.NewGuid();
        var a = Epic.Create(teamId, "Checkout", null, Now);
        var b = Epic.Create(teamId, "Payments v2", "desc", Now);
        _epics.Setup(x => x.GetByTeamAsync(teamId, It.IsAny<CancellationToken>())).ReturnsAsync([a, b]);
        _epics.Setup(x => x.GetTicketCountAsync(a.Id, It.IsAny<CancellationToken>())).ReturnsAsync(16);
        _epics.Setup(x => x.GetTicketCountAsync(b.Id, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var result = await _sut.Handle(new GetEpicsByTeamQuery(teamId), default);

        Assert.Equal(2, result.Count);
        Assert.Equal("Checkout", result[0].Title);
        Assert.Equal(16, result[0].TicketCount);
        Assert.Equal("Payments v2", result[1].Title);
        Assert.Equal("desc", result[1].Description);
        Assert.Equal(0, result[1].TicketCount);
    }
}
