using FluentValidation;
using TicketingSystem.Application.Common;

namespace TicketingSystem.Application.Commands.Tickets;

public class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.Type).Must(TicketEnumMap.IsValidType).WithMessage("Unknown ticket type.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Body).NotEmpty();
    }
}
