using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Domain.Tests.Entities;

public class CommentTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidInputs_SetsProperties()
    {
        var ticketId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        var comment = Comment.Create(ticketId, authorId, "Great progress!", Now);

        Assert.Equal(ticketId, comment.TicketId);
        Assert.Equal(authorId, comment.AuthorId);
        Assert.Equal("Great progress!", comment.Body);
        Assert.Equal(Now, comment.CreatedAt);
        Assert.NotEqual(Guid.Empty, comment.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyBody_ThrowsDomainException(string body)
    {
        Assert.Throws<DomainException>(() =>
            Comment.Create(Guid.NewGuid(), Guid.NewGuid(), body, Now));
    }
}
