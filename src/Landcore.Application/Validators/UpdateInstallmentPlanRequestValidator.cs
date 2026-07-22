using FluentValidation;
using Landcore.Application.DTOs;

namespace Landcore.Application.Validators;

public class UpdateInstallmentPlanRequestValidator : AbstractValidator<UpdateInstallmentPlanRequestDto>
{
    public UpdateInstallmentPlanRequestValidator()
    {
        RuleFor(x => x.Installments)
            .NotEmpty().WithMessage("At least one installment is required.");

        RuleForEach(x => x.Installments).ChildRules(installment =>
        {
            installment.RuleFor(i => i.Amount)
                .GreaterThan(0).WithMessage("Installment Amount must be greater than zero.");

            installment.RuleFor(i => i.DueDate)
                .NotEqual(default(DateTime)).WithMessage("Installment DueDate is required.");
        });
    }
}
