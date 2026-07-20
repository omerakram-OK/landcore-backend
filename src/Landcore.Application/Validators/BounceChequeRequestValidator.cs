using FluentValidation;
using Landcore.Application.DTOs;

namespace Landcore.Application.Validators;

public class BounceChequeRequestValidator : AbstractValidator<BounceChequeRequestDto>
{
    public BounceChequeRequestValidator()
    {
        RuleFor(x => x.PenaltyAmount)
            .GreaterThanOrEqualTo(0).WithMessage("PenaltyAmount must be zero or greater.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }
}
