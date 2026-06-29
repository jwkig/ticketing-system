using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Domain.Tests.Entities;

public class EpicTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid TeamId = Guid.NewGuid();

    [Fact]
    public void Create_ValidInputs_SetsProperties()
    {
        var epic = Epic.Create(TeamId, "  My Epic  ", "A description", Now);

        Assert.Equal(TeamId, epic.TeamId);
        Assert.Equal("My Epic", epic.Title);
        Assert.Equal("A description", epic.Description);
        Assert.Equal(Now, epic.CreatedAt);
        Assert.Equal(Now, epic.ModifiedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyTitle_ThrowsDomainException(string title)
    {
        Assert.Throws<DomainException>(() => Epic.Create(TeamId, title, null, Now));
    }

    [Fact]
    public void Update_ChangedFields_AdvancesModifiedAt()
    {
        var epic = Epic.Create(TeamId, "Original", null, Now);
        var later = Now.AddHours(1);

        epic.Update("Updated", "New desc", later);

        Assert.Equal("Updated", epic.Title);
        Assert.Equal("New desc", epic.Description);
        Assert.Equal(later, epic.ModifiedAt);
    }

    [Fact]
    public void Update_SameValues_DoesNotAdvanceModifiedAt()
    {
        var epic = Epic.Create(TeamId, "Original", "Desc", Now);
        var later = Now.AddHours(1);

        epic.Update("Original", "Desc", later);

        Assert.Equal(Now, epic.ModifiedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_EmptyTitle_ThrowsDomainException(string title)
    {
        var epic = Epic.Create(TeamId, "Original", null, Now);
        Assert.Throws<DomainException>(() => epic.Update(title, null, Now.AddHours(1)));
    }
}
