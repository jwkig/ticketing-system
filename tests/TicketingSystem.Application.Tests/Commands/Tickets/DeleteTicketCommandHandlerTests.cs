using TicketingSystem.Application.Commands.Tickets;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Tests.Commands.Tickets;

public class DeleteTicketCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly DeleteTicketCommandHandler _sut;

    public DeleteTicketCommandHandlerTests() => _sut = new DeleteTicketCommandHandler(_tickets.Object, _uow.Object);

    [Fact]
    public async Task Handle_UnknownTicket_ThrowsNotFound()
    {
        _tickets.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.Handle(new DeleteTicketCommand(Guid.NewGuid()), default));

        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingTicket_DeletesAndSaves()
    {
        var ticket = Ticket.Create(Guid.NewGuid(), null, null, Guid.NewGuid(), TicketType.Fix, "T", "B", Now);
        _tickets.Setup(x => x.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);

        await _sut.Handle(new DeleteTicketCommand(ticket.Id), default);

        _tickets.Verify(x => x.DeleteAsync(ticket, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
