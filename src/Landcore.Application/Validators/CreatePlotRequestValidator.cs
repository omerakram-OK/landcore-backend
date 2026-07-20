using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Validators;

public class CreatePlotRequestValidator : AbstractValidator<CreatePlotRequestDto>
{
    public CreatePlotRequestValidator()
    {
        RuleFor(x => x.PlotNumber)
            .NotEmpty().WithMessage("Plot number is required.")
            .MaximumLength(50);

        RuleFor(x => x.BlockId)
            .NotEmpty().WithMessage("BlockId is required.")
            .Must(id => ObjectId.TryParse(id, out _)).WithMessage("BlockId must be a valid identifier.");

        RuleFor(x => x.Size)
            .GreaterThan(0).WithMessage("Size must be greater than zero.");

        RuleFor(x => x.SizeUnit)
            .NotEmpty().WithMessage("SizeUnit is required.")
            .Must(unit => Enum.TryParse<PlotSizeUnit>(unit, ignoreCase: true, out _))
            .WithMessage($"SizeUnit must be one of: {string.Join(", ", Enum.GetNames<PlotSizeUnit>())}.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .Must(category => Enum.TryParse<PlotCategory>(category, ignoreCase: true, out _))
            .WithMessage($"Category must be one of: {string.Join(", ", Enum.GetNames<PlotCategory>())}.");

        RuleFor(x => x.BasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("BasePrice must be zero or greater.");

        RuleFor(x => x.AnnualMaintenanceCharge)
            .GreaterThanOrEqualTo(0).WithMessage("AnnualMaintenanceCharge must be zero or greater.");

        RuleForEach(x => x.Charges).SetValidator(new PlotChargeDtoValidator());

        RuleForEach(x => x.OwnerClientIds)
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Each OwnerClientId must be a valid identifier.");
    }
}

public class PlotChargeDtoValidator : AbstractValidator<PlotChargeDto>
{
    public PlotChargeDtoValidator()
    {
        RuleFor(x => x.ChargeType)
            .NotEmpty().WithMessage("ChargeType is required.")
            .MaximumLength(100);

        RuleFor(x => x.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("Amount must be zero or greater.");
    }
}
