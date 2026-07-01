using TicketingSystem.Application.Commands.Epics;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Tests.Commands.Epics;

public class CreateEpicCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IEpicRepository> _epics = new();
    private readonly Mock<ITeamRepository> _teams = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly CreateEpicCommandHandler _sut;

    public CreateEpicCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
        _sut = new CreateEpicCommandHandler(_epics.Object, _teams.Object, _clock.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_UnknownTeam_ThrowsNotFoundAndDoesNotSave()
    {
        _teams.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((Team?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.Handle(new CreateEpicCommand(Guid.NewGuid(), "Checkout", null), default));

        _epics.Verify(x => x.AddAsync(It.IsAny<Epic>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidTeam_CreatesTrimmedEpicAndSaves()
    {
        var team = Team.Create(new TeamName("Payments"), Now);
        _teams.Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);

        var result = await _sut.Handle(new CreateEpicCommand(team.Id, "  Checkout  ", "reliability"), default);

        Assert.Equal("Checkout", result.Title);
        Assert.Equal(team.Id, result.TeamId);
        Assert.Equal("reliability", result.Description);
        Assert.Equal(0, result.TicketCount);
        _epics.Verify(
            x => x.AddAsync(It.Is<Epic>(e => e.Title == "Checkout" && e.TeamId == team.Id), It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
