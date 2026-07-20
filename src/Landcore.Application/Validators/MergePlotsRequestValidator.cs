using FluentValidation;
using Landcore.Application.DTOs;
using MongoDB.Bson;

namespace Landcore.Application.Validators;

public class MergePlotsRequestValidator : AbstractValidator<MergePlotsRequestDto>
{
    public MergePlotsRequestValidator()
    {
        RuleFor(x => x.SourcePlotIds)
            .NotNull().WithMessage("SourcePlotIds is required.")
            .Must(ids => ids.Count >= 2).WithMessage("Merging Plots requires at least 2 source Plot ids.");

        RuleForEach(x => x.SourcePlotIds)
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Each SourcePlotId must be a valid identifier.");

        RuleFor(x => x.NewPlot)
            .NotNull().WithMessage("NewPlot is required.");

        RuleFor(x => x.NewPlot)
            .SetValidator(new NewPlotDefinitionValidator())
            .When(x => x.NewPlot is not null);

        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }
}
