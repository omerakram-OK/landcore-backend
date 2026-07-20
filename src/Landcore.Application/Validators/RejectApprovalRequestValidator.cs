using FluentValidation;
using Landcore.Application.DTOs;

namespace Landcore.Application.Validators;

public class RejectApprovalRequestValidator : AbstractValidator<RejectApprovalRequestDto>
{
    public RejectApprovalRequestValidator()
    {
        RuleFor(x => x.DecisionNotes)
            .NotEmpty().WithMessage("DecisionNotes (the rejection reason) is required.")
            .MaximumLength(1000);
    }
}
