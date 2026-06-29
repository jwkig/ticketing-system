using System.Text.RegularExpressions;
using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Domain.ValueObjects;

public sealed record EmailAddress
{
    private static readonly Regex Format = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    public EmailAddress(string value)
    {
        var normalised = value?.Trim().ToLowerInvariant()
            ?? throw new DomainException("Email must not be null.");

        if (normalised.Length == 0)
            throw new DomainException("Email must not be empty.");

        if (!Format.IsMatch(normalised))
            throw new DomainException($"'{value}' is not a valid email address.");

        Value = normalised;
    }

    public override string ToString() => Value;
}
