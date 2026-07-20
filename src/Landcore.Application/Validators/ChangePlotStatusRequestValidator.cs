using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Domain.Enums;

namespace Landcore.Application.Validators;

public class ChangePlotStatusRequestValidator : AbstractValidator<ChangePlotStatusRequestDto>
{
    public ChangePlotStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(status => Enum.TryParse<PlotStatus>(status, ignoreCase: true, out _))
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<PlotStatus>())}.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }
}
