using FluentValidation;

namespace TicketingSystem.Application.Commands.Epics;

public class DeleteEpicCommandValidator : AbstractValidator<DeleteEpicCommand>
{
    public DeleteEpicCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
