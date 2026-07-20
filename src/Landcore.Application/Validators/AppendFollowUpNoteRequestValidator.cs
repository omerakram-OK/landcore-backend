using FluentValidation;
using Landcore.Application.DTOs;

namespace Landcore.Application.Validators;

public class AppendFollowUpNoteRequestValidator : AbstractValidator<AppendFollowUpNoteRequestDto>
{
    public AppendFollowUpNoteRequestValidator()
    {
        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("Note is required.")
            .MaximumLength(2000);
    }
}
