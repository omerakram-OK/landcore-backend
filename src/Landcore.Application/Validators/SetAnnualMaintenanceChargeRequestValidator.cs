using FluentValidation;
using Landcore.Application.DTOs;

namespace Landcore.Application.Validators;

public class SetAnnualMaintenanceChargeRequestValidator : AbstractValidator<SetAnnualMaintenanceChargeRequestDto>
{
    public SetAnnualMaintenanceChargeRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Amount must be zero or greater.");
    }
}
