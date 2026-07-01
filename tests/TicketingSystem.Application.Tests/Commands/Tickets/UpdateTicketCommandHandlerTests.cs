using TicketingSystem.Application.Commands.Tickets;
using TicketingSystem.Application.Services;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.Repositories;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Tests.Commands.Tickets;

public class UpdateTicketCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TeamId = Guid.NewGuid();

    private readonly Mock<ITicketRepository> _tickets = new();
    private readonly Mock<IEpicRepository> _epics = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly UpdateTicketCommandHandler _sut;

    public UpdateTicketCommandHandlerTests()
    {
        _clock.Setup(x => x.UtcNow).Returns(Now);
        _sut = new UpdateTicketCommandHandler(_tickets.Object, _epics.Object, _clock.Object, _uow.Object);
    }

    private Ticket SetupTicket() =>
        SetupTicket(Ticket.Create(TeamId, null, null, Guid.NewGuid(), TicketType.Bug, "Old", "old body", Now));

    private Ticket SetupTicket(Ticket ticket)
    {
        _tickets.Setup(x => x.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>())).ReturnsAsync(ticket);
        return ticket;
    }

    [Fact]
    public async Task Handle_UnknownTicket_ThrowsNotFound()
    {
        _tickets.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Ticket?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.Handle(new UpdateTicketCommand(Guid.NewGuid(), "bug", "T", "B", null), default));
    }

    [Fact]
    public async Task Handle_ChangesTypeTitleBodyAndSaves()
    {
        var ticket = SetupTicket();

        var result = await _sut.Handle(new UpdateTicketCommand(ticket.Id, "feature", "New title", "new body", null), default);

        Assert.Equal("feature", result.Type);
        Assert.Equal("New title", result.Title);
        Assert.Equal("new body", result.Body);
        Assert.Equal(TeamId, result.TeamId);
        _tickets.Verify(x => x.UpdateAsync(ticket, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AttachEpicOnSameTeam_SetsEpicIdAndTitle()
    {
        var ticket = SetupTicket();
        var epic = Epic.Create(TeamId, "Checkout", null, Now);
        _epics.Setup(x => x.GetByIdAsync(epic.Id, It.IsAny<CancellationToken>())).ReturnsAsync(epic);

        var result = await _sut.Handle(new UpdateTicketCommand(ticket.Id, "bug", "T", "B", epic.Id), default);

        Assert.Equal(epic.Id, result.EpicId);
        Assert.Equal("Checkout", result.EpicTitle);
    }

    [Fact]
    public async Task Handle_EpicFromAnotherTeam_ThrowsDomainException()
    {
        var ticket = SetupTicket();
        var otherEpic = Epic.Create(Guid.NewGuid(), "Other", null, Now);
        _epics.Setup(x => x.GetByIdAsync(otherEpic.Id, It.IsAny<CancellationToken>())).ReturnsAsync(otherEpic);

        await Assert.ThrowsAsync<DomainException>(
            () => _sut.Handle(new UpdateTicketCommand(ticket.Id, "bug", "T", "B", otherEpic.Id), default));

        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
