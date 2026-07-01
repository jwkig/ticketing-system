using TicketingSystem.Application.Commands.Teams;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Application.Tests.Commands.Teams;

public class CreateTeamCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ITeamRepository> _teams = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly CreateTeamCommandHandler _sut;

    public CreateTeamCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
        _sut = new CreateTeamCommandHandler(_teams.Object, _clock.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_UniqueName_CreatesTrimmedTeamAndSaves()
    {
        _teams.Setup(x => x.ExistsByNameAsync("Backend", null, It.IsAny<CancellationToken>()))
              .ReturnsAsync(false);

        var result = await _sut.Handle(new CreateTeamCommand("  Backend  "), default);

        Assert.Equal("Backend", result.Name);
        Assert.Equal(Now, result.CreatedAt);
        Assert.Equal(0, result.TicketCount);
        Assert.Equal(0, result.EpicCount);
        _teams.Verify(x => x.AddAsync(It.Is<Team>(t => t.Name.Value == "Backend"), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateName_ThrowsConflictAndDoesNotSave()
    {
        _teams.Setup(x => x.ExistsByNameAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
              .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.Handle(new CreateTeamCommand("Backend"), default));

        _teams.Verify(x => x.AddAsync(It.IsAny<Team>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
