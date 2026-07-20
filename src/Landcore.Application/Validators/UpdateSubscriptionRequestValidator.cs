using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Domain.Enums;

namespace Landcore.Application.Validators;

public class UpdateSubscriptionRequestValidator : AbstractValidator<UpdateSubscriptionRequestDto>
{
    public UpdateSubscriptionRequestValidator()
    {
        RuleFor(x => x.Plan)
            .NotEmpty().WithMessage("Plan is required.")
            .Must(plan => Enum.TryParse<SubscriptionPlan>(plan, ignoreCase: true, out _))
            .WithMessage($"Plan must be one of: {string.Join(", ", Enum.GetNames<SubscriptionPlan>())}.");

        RuleFor(x => x.FeeAmount)
            .GreaterThan(0).WithMessage("FeeAmount must be greater than zero.");

        RuleFor(x => x.NextDueDate)
            .GreaterThan(x => x.StartDate).WithMessage("NextDueDate must be after StartDate.");
    }
}
