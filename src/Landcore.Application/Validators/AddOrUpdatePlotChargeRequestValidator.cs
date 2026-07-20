using FluentValidation;
using Landcore.Application.DTOs;

namespace Landcore.Application.Validators;

public class AddOrUpdatePlotChargeRequestValidator : AbstractValidator<AddOrUpdatePlotChargeRequestDto>
{
    public AddOrUpdatePlotChargeRequestValidator()
    {
        RuleFor(x => x.ChargeType)
            .NotEmpty().WithMessage("ChargeType is required.")
            .MaximumLength(100);

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Amount must be zero or greater.");
    }
}
