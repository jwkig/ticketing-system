using TicketingSystem.Domain.Exceptions;

namespace TicketingSystem.Domain.ValueObjects;

public sealed record TeamName
{
    public string Value { get; }

    public TeamName(string value)
    {
        var trimmed = value?.Trim()
            ?? throw new DomainException("Team name must not be null.");

        if (trimmed.Length == 0)
            throw new DomainException("Team name must not be empty.");

        Value = trimmed;
    }

    public override string ToString() => Value;
}
