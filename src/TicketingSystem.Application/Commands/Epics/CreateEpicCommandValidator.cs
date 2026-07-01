using FluentValidation;

namespace TicketingSystem.Application.Commands.Epics;

public class CreateEpicCommandValidator : AbstractValidator<CreateEpicCommand>
{
    public CreateEpicCommandValidator()
    {
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
    }
}
