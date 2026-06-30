using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;
using TicketingSystem.Application.Services;
using TicketingSystem.Infrastructure.Options;

namespace TicketingSystem.Infrastructure.Services;

public sealed class ArgonPasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;

    private readonly Argon2HashingSettings _settings;

    public ArgonPasswordHasher(IOptions<Argon2HashingSettings> settings) =>
        _settings = settings.Value;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(password, salt);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string passwordHash, string password)
    {
        var parts = passwordHash.Split(':');
        if (parts.Length != 2) return false;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[0]);
            expected = Convert.FromBase64String(parts[1]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = ComputeHash(password, salt);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private byte[] ComputeHash(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = _settings.MemorySize,
            Iterations = _settings.Iterations,
            DegreeOfParallelism = _settings.DegreeOfParallelism
        };
        return argon2.GetBytes(HashSize);
    }
}
