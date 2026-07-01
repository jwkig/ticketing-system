using TicketingSystem.Application.Commands.Tickets;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Tests.Commands.Tickets;

public class CreateTicketCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid UserId = Guid.NewGuid();

    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IEpicRepository> _epics = new();
    private readonly Mock<ITeamRepository> _teams = new();
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly CreateTicketCommandHandler _sut;

    public CreateTicketCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
        _currentUser.Setup(x => x.UserId).Returns(UserId);
        _sut = new CreateTicketCommandHandler(
            _tickets.Object, _epics.Object, _teams.Object, _currentUser.Object, _clock.Object, _uow.Object);
    }

    private Team SetupTeam()
    {
        var team = Team.Create(new TeamName("Payments"), Now);
        _teams.Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        return team;
    }

    [Fact]
    public async Task Handle_UnknownTeam_ThrowsNotFoundAndDoesNotSave()
    {
        _teams.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Team?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.Handle(new CreateTicketCommand(Guid.NewGuid(), "bug", "Title", "Body", null), default));

        _tickets.Verify(x => x.AddAsync(It.IsAny<Ticket>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoEpic_CreatesTicketInNewStateWithCurrentUserAndSaves()
    {
        var team = SetupTeam();

        var result = await _sut.Handle(new CreateTicketCommand(team.Id, "feature", "Dark mode", "As a user…", null), default);

        Assert.Equal("feature", result.Type);
        Assert.Equal("new", result.State);
        Assert.Equal("Dark mode", result.Title);
        Assert.Equal("As a user…", result.Body);
        Assert.Equal(team.Id, result.TeamId);
        Assert.Null(result.EpicId);
        Assert.Null(result.EpicTitle);
        Assert.Equal(UserId, result.CreatedById);
        _tickets.Verify(
            x => x.AddAsync(It.Is<Ticket>(t => t.CreatedById == UserId && t.State == TicketState.New), It.IsAny<CancellationToken>()),
            Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEpicOnSameTeam_SetsEpicIdAndTitle()
    {
        var team = SetupTeam();
        var epic = Epic.Create(team.Id, "Checkout", null, Now);
        _epics.Setup(x => x.GetByIdAsync(epic.Id, It.IsAny<CancellationToken>())).ReturnsAsync(epic);

        var result = await _sut.Handle(new CreateTicketCommand(team.Id, "bug", "Login fails", "steps", epic.Id), default);

        Assert.Equal(epic.Id, result.EpicId);
        Assert.Equal("Checkout", result.EpicTitle);
    }

    [Fact]
    public async Task Handle_EpicNotFound_ThrowsNotFound()
    {
        var team = SetupTeam();
        _epics.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Epic?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.Handle(new CreateTicketCommand(team.Id, "bug", "T", "B", Guid.NewGuid()), default));

        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EpicFromAnotherTeam_ThrowsDomainException()
    {
        var team = SetupTeam();
        var otherTeamEpic = Epic.Create(Guid.NewGuid(), "Other", null, Now);
        _epics.Setup(x => x.GetByIdAsync(otherTeamEpic.Id, It.IsAny<CancellationToken>())).ReturnsAsync(otherTeamEpic);

        await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new CreateTicketCommand(team.Id, "bug", "T", "B", otherTeamEpic.Id), default));

        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
