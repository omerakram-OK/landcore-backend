using FluentValidation;
using Landcore.Application.DTOs;

namespace Landcore.Application.Validators;

public class SplitPlotRequestValidator : AbstractValidator<SplitPlotRequestDto>
{
    public SplitPlotRequestValidator()
    {
        RuleFor(x => x.NewPlots)
            .NotNull().WithMessage("NewPlots is required.")
            .Must(plots => plots.Count >= 2).WithMessage("Splitting a Plot requires at least 2 new Plot definitions.");

        RuleForEach(x => x.NewPlots).SetValidator(new NewPlotDefinitionValidator());

        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }
}
