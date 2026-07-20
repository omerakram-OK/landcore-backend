using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Validators;

public class CreateSubscriptionRequestValidator : AbstractValidator<CreateSubscriptionRequestDto>
{
    public CreateSubscriptionRequestValidator()
    {
        RuleFor(x => x.AdminId)
            .NotEmpty().WithMessage("AdminId is required.")
            .Must(id => ObjectId.TryParse(id, out _)).WithMessage("AdminId must be a valid identifier.");

        RuleFor(x => x.Plan)
            .NotEmpty().WithMessage("Plan is required.")
            .Must(plan => Enum.TryParse<SubscriptionPlan>(plan, ignoreCase: true, out _))
            .WithMessage($"Plan must be one of: {string.Join(", ", Enum.GetNames<SubscriptionPlan>())}.");

        RuleFor(x => x.FeeAmount)
            .GreaterThan(0).WithMessage("FeeAmount must be greater than zero.");

        RuleFor(x => x.NextDueDate)
            .GreaterThan(x => x.StartDate).WithMessage("NextDueDate must be after StartDate.");

        RuleFor(x => x.Status)
            .Must(status => status is null || Enum.TryParse<SubscriptionStatus>(status, ignoreCase: true, out _))
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<SubscriptionStatus>())}.");
    }
}
