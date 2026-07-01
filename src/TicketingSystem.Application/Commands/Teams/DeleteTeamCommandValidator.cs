using FluentValidation;

namespace TicketingSystem.Application.Commands.Teams;

public class DeleteTeamCommandValidator : AbstractValidator<DeleteTeamCommand>
{
    public DeleteTeamCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
