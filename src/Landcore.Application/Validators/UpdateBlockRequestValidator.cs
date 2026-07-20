using FluentValidation;
using Landcore.Application.DTOs;

namespace Landcore.Application.Validators;

public class UpdateBlockRequestValidator : AbstractValidator<UpdateBlockRequestDto>
{
    public UpdateBlockRequestValidator()
    {
        RuleFor(x => x.SocietyId)
            .NotEmpty().WithMessage("SocietyId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Block name/number is required.")
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .NotNull().WithMessage("Description must not be null (an empty string is allowed).")
            .MaximumLength(1000);

        RuleFor(x => x.TotalPlots)
            .GreaterThanOrEqualTo(0).WithMessage("TotalPlots must be zero or greater.");
    }
}
