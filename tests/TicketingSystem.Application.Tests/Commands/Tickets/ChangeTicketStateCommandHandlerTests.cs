using TicketingSystem.Application.Commands.Tickets;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Tests.Commands.Tickets;

public class ChangeTicketStateCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IEpicRepository> _epics = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly ChangeTicketStateCommandHandler _sut;

    public ChangeTicketStateCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
        _sut = new ChangeTicketStateCommandHandler(_tickets.Object, _epics.Object, _clock.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_UnknownTicket_ThrowsNotFound()
    {
        _tickets.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.Handle(new ChangeTicketStateCommand(Guid.NewGuid(), "in_progress"), default));
    }

    [Fact]
    public async Task Handle_MovesTicketToNewStateAndSaves()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), null, null, Guid.NewGuid(), TicketType.Bug, "T", "B", Now);
        _tickets.Setup(x => x.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        var result = await _sut.Handle(new ChangeTicketStateCommand(ticket.Id, "in_progress"), default);

        Assert.Equal("in_progress", result.State);
        Assert.Equal(TicketState.InProgress, ticket.State);
        _tickets.Verify(x => x.UpdateAsync(ticket, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
