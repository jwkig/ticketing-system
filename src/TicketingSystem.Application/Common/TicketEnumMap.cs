using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Application.Common;

/// <summary>
/// Maps ticket enums to/from the canonical snake_case API strings
/// (<c>bug|feature|fix</c>, <c>new|ready_for_implementation|in_progress|ready_for_acceptance|done</c>).
/// </summary>
public static class TicketEnumMap
{
    public static string ToApiString(TicketType type) => type switch
    {
        TicketType.Bug => "bug",
        TicketType.Feature => "feature",
        TicketType.Fix => "fix",
        _ => type.ToString().ToLowerInvariant(),
    };

    public static string ToApiString(TicketState state) => state switch
    {
        TicketState.New => "new",
        TicketState.ReadyForImplementation => "ready_for_implementation",
        TicketState.InProgress => "in_progress",
        TicketState.ReadyForAcceptance => "ready_for_acceptance",
        TicketState.Done => "done",
        _ => state.ToString().ToLowerInvariant(),
    };

    public static bool IsValidType(string? value) => TryParseType(value, out _);

    public static bool IsValidState(string? value) => TryParseState(value, out _);

    public static bool TryParseType(string? value, out TicketType type)
    {
        switch (value)
        {
            case "bug": type = TicketType.Bug; return true;
            case "feature": type = TicketType.Feature; return true;
            case "fix": type = TicketType.Fix; return true;
            default: type = default; return false;
        }
    }

    public static bool TryParseState(string? value, out TicketState state)
    {
        switch (value)
        {
            case "new": state = TicketState.New; return true;
            case "ready_for_implementation": state = TicketState.ReadyForImplementation; return true;
            case "in_progress": state = TicketState.InProgress; return true;
            case "ready_for_acceptance": state = TicketState.ReadyForAcceptance; return true;
            case "done": state = TicketState.Done; return true;
            default: state = default; return false;
        }
    }

    public static TicketType ParseType(string value) =>
        TryParseType(value, out var type) ? type : throw new ArgumentException($"Unknown ticket type '{value}'.");

    public static TicketState ParseState(string value) =>
        TryParseState(value, out var state) ? state : throw new ArgumentException($"Unknown ticket state '{value}'.");
}
