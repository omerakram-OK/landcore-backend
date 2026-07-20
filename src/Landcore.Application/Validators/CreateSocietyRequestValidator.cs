using FluentValidation;
using Landcore.Application.DTOs;

namespace Landcore.Application.Validators;

public class CreateSocietyRequestValidator : AbstractValidator<CreateSocietyRequestDto>
{
    public CreateSocietyRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Society name is required.")
            .MaximumLength(150);

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500);

        RuleFor(x => x.Description)
            .NotNull().WithMessage("Description must not be null (an empty string is allowed).")
            .MaximumLength(1000);

        RuleFor(x => x.TotalPlots)
            .GreaterThanOrEqualTo(0).WithMessage("TotalPlots must be zero or greater.");
    }
}
