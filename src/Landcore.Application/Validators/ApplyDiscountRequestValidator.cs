using FluentValidation;
using Landcore.Application.DTOs;

namespace Landcore.Application.Validators;

public class ApplyDiscountRequestValidator : AbstractValidator<ApplyDiscountRequestDto>
{
    public ApplyDiscountRequestValidator()
    {
        RuleFor(x => x.DiscountAmount)
            .GreaterThan(0).WithMessage("DiscountAmount must be greater than 0.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }
}
