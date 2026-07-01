using FluentValidation;
using TicketingSystem.Application.Common;

namespace TicketingSystem.Application.Commands.Tickets;

public class ChangeTicketStateCommandValidator : AbstractValidator<ChangeTicketStateCommand>
{
    public ChangeTicketStateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.State).Must(TicketEnumMap.IsValidState).WithMessage("Unknown ticket state.");
    }
}
