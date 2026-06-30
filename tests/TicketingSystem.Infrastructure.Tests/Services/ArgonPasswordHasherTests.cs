using TicketingSystem.Infrastructure.Options;
using TicketingSystem.Infrastructure.Services;

namespace TicketingSystem.Infrastructure.Tests.Services;

public class ArgonPasswordHasherTests
{
    // Minimal Argon2 parameters so tests run in under a second.
    private static readonly Argon2HashingSettings FastSettings = new()
    {
        MemorySize = 256,
        Iterations = 1,
        DegreeOfParallelism = 1
    };

    private readonly ArgonPasswordHasher _sut =
        new(Microsoft.Extensions.Options.Options.Create(FastSettings));

    [Fact]
    public void Hash_ReturnsNonEmptyString()
    {
        var result = _sut.Hash("Password1!");
        Assert.False(string.IsNullOrWhiteSpace(result));
    }

    [Fact]
    public void Hash_ContainsSaltAndHashSeparatedByColon()
    {
        var result = _sut.Hash("Password1!");
        var parts = result.Split(':');
        Assert.Equal(2, parts.Length);
        Assert.All(parts, p => Assert.False(string.IsNullOrWhiteSpace(p)));
    }

    [Fact]
    public void Hash_SamePasswordProducesDifferentOutputsEachTime()
    {
        var hash1 = _sut.Hash("Password1!");
        var hash2 = _sut.Hash("Password1!");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _sut.Hash("correct-password");
        Assert.True(_sut.Verify(hash, "correct-password"));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _sut.Hash("correct-password");
        Assert.False(_sut.Verify(hash, "wrong-password"));
    }

    [Fact]
    public void Verify_MalformedHash_ReturnsFalse()
    {
        Assert.False(_sut.Verify("not-a-valid-hash", "password"));
    }

    [Fact]
    public void Verify_InvalidBase64_ReturnsFalse()
    {
        Assert.False(_sut.Verify("!!!:!!!", "password"));
    }

    [Fact]
    public void Verify_EmptyHash_ReturnsFalse()
    {
        Assert.False(_sut.Verify(string.Empty, "password"));
    }
}
