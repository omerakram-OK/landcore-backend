using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Domain.Enums;

namespace Landcore.Application.Validators;

public class ProposeApprovalRequestValidator : AbstractValidator<ProposeApprovalRequestDto>
{
    public ProposeApprovalRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Type is required.")
            .Must(value => Enum.TryParse<ApprovalRequestType>(value, ignoreCase: true, out _))
            .WithMessage($"Type must be one of: {string.Join(", ", Enum.GetNames<ApprovalRequestType>())}.");

        RuleFor(x => x.Justification)
            .NotEmpty().WithMessage("Justification is required.")
            .MaximumLength(1000);
    }
}
