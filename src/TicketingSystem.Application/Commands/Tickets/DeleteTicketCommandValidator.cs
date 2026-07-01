using FluentValidation;

namespace TicketingSystem.Application.Commands.Tickets;

public class DeleteTicketCommandValidator : AbstractValidator<DeleteTicketCommand>
{
    public DeleteTicketCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
