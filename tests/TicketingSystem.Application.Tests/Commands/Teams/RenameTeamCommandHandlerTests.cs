using TicketingSystem.Application.Commands.Teams;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Tests.Commands.Teams;

public class RenameTeamCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ITeamRepository> _teams = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly RenameTeamCommandHandler _sut;

    public RenameTeamCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now.AddHours(1));
        _sut = new RenameTeamCommandHandler(_teams.Object, _clock.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_UnknownTeam_ThrowsNotFound()
    {
        _teams.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((Team?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.Handle(new RenameTeamCommand(Guid.NewGuid(), "Frontend"), default));
    }

    [Fact]
    public async Task Handle_DuplicateName_ThrowsConflict()
    {
        var team = Team.Create(new TeamName("Backend"), Now);
        _teams.Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        _teams.Setup(x => x.ExistsByNameAsync("Frontend", team.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.Handle(new RenameTeamCommand(team.Id, "Frontend"), default));
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Valid_RenamesReturnsDtoWithCounts()
    {
        var team = Team.Create(new TeamName("Backend"), Now);
        _teams.Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        _teams.Setup(x => x.ExistsByNameAsync("Frontend", team.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);
        _teams.Setup(x => x.GetReferenceCountsAsync(team.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new TeamReferenceCounts(3, 2));

        var result = await _sut.Handle(new RenameTeamCommand(team.Id, "Frontend"), default);

        Assert.Equal("Frontend", result.Name);
        Assert.Equal(Now.AddHours(1), result.ModifiedAt);
        Assert.Equal(3, result.TicketCount);
        Assert.Equal(2, result.EpicCount);
        _teams.Verify(x => x.UpdateAsync(team, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
