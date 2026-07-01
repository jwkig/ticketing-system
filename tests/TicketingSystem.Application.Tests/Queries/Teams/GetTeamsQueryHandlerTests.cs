using TicketingSystem.Application.Queries.Teams;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Tests.Queries.Teams;

public class GetTeamsQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ITeamRepository> _teams = new();
    private readonly GetTeamsQueryHandler _sut;

    public GetTeamsQueryHandlerTests() => _sut = new GetTeamsQueryHandler(_teams.Object);

    [Fact]
    public async Task Handle_MapsTeamsWithReferenceCounts()
    {
        var a = Team.Create(new TeamName("Backend"), Now);
        var b = Team.Create(new TeamName("Frontend"), Now);
        _teams.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
              .ReturnsAsync([a, b]);
        _teams.Setup(x => x.GetReferenceCountsAsync(a.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new TeamReferenceCounts(5, 2));
        _teams.Setup(x => x.GetReferenceCountsAsync(b.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new TeamReferenceCounts(0, 0));

        var result = await _sut.Handle(new GetTeamsQuery(), default);

        Assert.Equal(2, result.Count);
        Assert.Equal("Backend", result[0].Name);
        Assert.Equal(5, result[0].TicketCount);
        Assert.Equal(2, result[0].EpicCount);
        Assert.Equal("Frontend", result[1].Name);
        Assert.Equal(0, result[1].TicketCount);
    }
}
