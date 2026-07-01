using TicketingSystem.Application.Commands.Epics;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;

namespace TicketingSystem.Application.Tests.Commands.Epics;

public class DeleteEpicCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IEpicRepository> _epics = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly DeleteEpicCommandHandler _sut;

    public DeleteEpicCommandHandlerTests() => _sut = new DeleteEpicCommandHandler(_epics.Object, _uow.Object);

    private static Epic NewEpic() => Epic.Create(Guid.NewGuid(), "Checkout", null, Now);

    [Fact]
    public async Task Handle_UnknownEpic_ThrowsNotFound()
    {
        _epics.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync((Epic?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.Handle(new DeleteEpicCommand(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task Handle_EpicWithTickets_ThrowsConflictAndDoesNotDelete()
    {
        var epic = NewEpic();
        _epics.Setup(x => x.GetByIdAsync(epic.Id, It.IsAny<CancellationToken>())).ReturnsAsync(epic);
        _epics.Setup(x => x.HasTicketsAsync(epic.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.Handle(new DeleteEpicCommand(epic.Id), default));

        _epics.Verify(x => x.DeleteAsync(It.IsAny<Epic>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EpicWithoutTickets_DeletesAndSaves()
    {
        var epic = NewEpic();
        _epics.Setup(x => x.GetByIdAsync(epic.Id, It.IsAny<CancellationToken>())).ReturnsAsync(epic);
        _epics.Setup(x => x.HasTicketsAsync(epic.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await _sut.Handle(new DeleteEpicCommand(epic.Id), default);

        _epics.Verify(x => x.DeleteAsync(epic, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
