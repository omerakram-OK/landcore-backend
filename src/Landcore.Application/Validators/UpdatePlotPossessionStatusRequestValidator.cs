using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Domain.Enums;

namespace Landcore.Application.Validators;

public class UpdatePlotPossessionStatusRequestValidator : AbstractValidator<UpdatePlotPossessionStatusRequestDto>
{
    public UpdatePlotPossessionStatusRequestValidator()
    {
        RuleFor(x => x.PossessionStatus)
            .NotEmpty().WithMessage("PossessionStatus is required.")
            .Must(status => Enum.TryParse<PossessionStatus>(status, ignoreCase: true, out _))
            .WithMessage($"PossessionStatus must be one of: {string.Join(", ", Enum.GetNames<PossessionStatus>())}.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }
}
