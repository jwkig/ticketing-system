using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Domain.Tests.Entities;

public class TeamTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_SetsPropertiesCorrectly()
    {
        var name = new TeamName("Backend");
        var team = Team.Create(name, Now);

        Assert.Equal(name, team.Name);
        Assert.Equal(Now, team.CreatedAt);
        Assert.Equal(Now, team.ModifiedAt);
        Assert.NotEqual(Guid.Empty, team.Id);
    }

    [Fact]
    public void Rename_DifferentName_UpdatesNameAndModifiedAt()
    {
        var team = Team.Create(new TeamName("Backend"), Now);
        var later = Now.AddHours(1);

        team.Rename(new TeamName("Frontend"), later);

        Assert.Equal("Frontend", team.Name.Value);
        Assert.Equal(later, team.ModifiedAt);
    }

    [Fact]
    public void Rename_SameName_DoesNotAdvanceModifiedAt()
    {
        var name = new TeamName("Backend");
        var team = Team.Create(name, Now);
        var later = Now.AddHours(1);

        team.Rename(new TeamName("Backend"), later);

        Assert.Equal(Now, team.ModifiedAt);
    }
}
