using TicketingSystem.Application.Commands.Epics;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Application.Tests.Commands.Epics;

public class UpdateEpicCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IEpicRepository> _epics = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly UpdateEpicCommandHandler _sut;

    public UpdateEpicCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now.AddHours(1));
        _sut = new UpdateEpicCommandHandler(_epics.Object, _clock.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_UnknownEpic_ThrowsNotFound()
    {
        _epics.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((Epic?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.Handle(new UpdateEpicCommand(Guid.NewGuid(), "New", null), default));
    }

    [Fact]
    public async Task Handle_Valid_UpdatesTitleReturnsDtoWithCount()
    {
        var epic = Epic.Create(Guid.NewGuid(), "Old", "old desc", Now);
        _epics.Setup(x => x.GetByIdAsync(epic.Id, It.IsAny<CancellationToken>())).ReturnsAsync(epic);
        _epics.Setup(x => x.GetTicketCountAsync(epic.Id, It.IsAny<CancellationToken>())).ReturnsAsync(4);

        var result = await _sut.Handle(new UpdateEpicCommand(epic.Id, "New title", "new desc"), default);

        Assert.Equal("New title", result.Title);
        Assert.Equal("new desc", result.Description);
        Assert.Equal(Now.AddHours(1), result.ModifiedAt);
        Assert.Equal(4, result.TicketCount);
        _epics.Verify(x => x.UpdateAsync(epic, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
