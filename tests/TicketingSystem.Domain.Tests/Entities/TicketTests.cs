using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Domain.Tests.Entities;

public class TicketTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TeamId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static Ticket DefaultTicket(DateTimeOffset? at = null) =>
        Ticket.Create(TeamId, null, null, UserId, TicketType.Feature, "My Ticket", "Body text", at ?? Now);

    [Fact]
    public void Create_ValidInputs_SetsProperties()
    {
        var ticket = DefaultTicket();

        Assert.Equal(TeamId, ticket.TeamId);
        Assert.Equal(UserId, ticket.CreatedById);
        Assert.Equal(TicketType.Feature, ticket.Type);
        Assert.Equal(TicketState.New, ticket.State);
        Assert.Equal("My Ticket", ticket.Title);
        Assert.Equal("Body text", ticket.Body);
        Assert.Equal(Now, ticket.CreatedAt);
        Assert.Equal(Now, ticket.ModifiedAt);
        Assert.Null(ticket.EpicId);
    }

    [Fact]
    public void Create_TitleIsTrimmed()
    {
        var ticket = Ticket.Create(TeamId, null, null, UserId, TicketType.Bug, "  Trimmed  ", "body", Now);
        Assert.Equal("Trimmed", ticket.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyTitle_ThrowsDomainException(string title)
    {
        Assert.Throws<DomainException>(() =>
            Ticket.Create(TeamId, null, null, UserId, TicketType.Bug, title, "body", Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyBody_ThrowsDomainException(string body)
    {
        Assert.Throws<DomainException>(() =>
            Ticket.Create(TeamId, null, null, UserId, TicketType.Bug, "title", body, Now));
    }

    [Fact]
    public void Create_EpicFromDifferentTeam_ThrowsDomainException()
    {
        var epicId = Guid.NewGuid();
        var differentTeamId = Guid.NewGuid();

        Assert.Throws<DomainException>(() =>
            Ticket.Create(TeamId, epicId, differentTeamId, UserId, TicketType.Bug, "title", "body", Now));
    }

    [Fact]
    public void Create_EpicFromSameTeam_Succeeds()
    {
        var epicId = Guid.NewGuid();
        var ticket = Ticket.Create(TeamId, epicId, TeamId, UserId, TicketType.Feature, "title", "body", Now);
        Assert.Equal(epicId, ticket.EpicId);
    }

    [Fact]
    public void Update_ChangedFields_AdvancesModifiedAt()
    {
        var ticket = DefaultTicket();
        var later = Now.AddHours(1);

        ticket.Update(TeamId, null, null, TicketType.Bug, "New Title", "New Body", later);

        Assert.Equal(TicketType.Bug, ticket.Type);
        Assert.Equal("New Title", ticket.Title);
        Assert.Equal("New Body", ticket.Body);
        Assert.Equal(later, ticket.ModifiedAt);
    }

    [Fact]
    public void Update_UnchangedFields_DoesNotAdvanceModifiedAt()
    {
        var ticket = DefaultTicket();
        var later = Now.AddHours(1);

        ticket.Update(TeamId, null, null, TicketType.Feature, "My Ticket", "Body text", later);

        Assert.Equal(Now, ticket.ModifiedAt);
    }

    [Fact]
    public void Update_EpicFromDifferentTeam_ThrowsDomainException()
    {
        var ticket = DefaultTicket();
        var epicId = Guid.NewGuid();
        var differentTeamId = Guid.NewGuid();

        Assert.Throws<DomainException>(() =>
            ticket.Update(TeamId, epicId, differentTeamId, TicketType.Feature, "title", "body", Now.AddHours(1)));
    }

    [Fact]
    public void SetState_DifferentState_UpdatesStateAndModifiedAt()
    {
        var ticket = DefaultTicket();
        var later = Now.AddHours(1);

        ticket.SetState(TicketState.InProgress, later);

        Assert.Equal(TicketState.InProgress, ticket.State);
        Assert.Equal(later, ticket.ModifiedAt);
    }

    [Fact]
    public void SetState_SameState_DoesNotAdvanceModifiedAt()
    {
        var ticket = DefaultTicket();
        var later = Now.AddHours(1);

        ticket.SetState(TicketState.New, later);

        Assert.Equal(Now, ticket.ModifiedAt);
    }
}
