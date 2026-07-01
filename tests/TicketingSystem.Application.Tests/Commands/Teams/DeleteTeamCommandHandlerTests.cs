using TicketingSystem.Application.Commands.Teams;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Tests.Commands.Teams;

public class DeleteTeamCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ITeamRepository> _teams = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly DeleteTeamCommandHandler _sut;

    public DeleteTeamCommandHandlerTests() => _sut = new DeleteTeamCommandHandler(_teams.Object, _uow.Object);

    [Fact]
    public async Task Handle_UnknownTeam_ThrowsNotFound()
    {
        _teams.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((Team?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.Handle(new DeleteTeamCommand(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_TeamWithTicketsOrEpics_ThrowsConflictAndDoesNotDelete()
    {
        var team = Team.Create(new TeamName("Backend"), Now);
        _teams.Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        _teams.Setup(x => x.HasTicketsOrEpicsAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.Handle(new DeleteTeamCommand(team.Id), default));

        _teams.Verify(x => x.DeleteAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyTeam_DeletesAndSaves()
    {
        var team = Team.Create(new TeamName("Backend"), Now);
        _teams.Setup(x => x.GetByIdAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(team);
        _teams.Setup(x => x.HasTicketsOrEpicsAsync(team.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _sut.Handle(new DeleteTeamCommand(team.Id), default);

        _teams.Verify(x => x.DeleteAsync(team, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
