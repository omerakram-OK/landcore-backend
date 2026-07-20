using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Validators;

public class UpdateLeadRequestValidator : AbstractValidator<UpdateLeadRequestDto>
{
    public UpdateLeadRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .MustBeValidPakistaniPhone();

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Source)
            .NotEmpty().WithMessage("Source is required.")
            .Must(source => Enum.TryParse<LeadSource>(source, ignoreCase: true, out _))
            .WithMessage($"Source must be one of: {string.Join(", ", Enum.GetNames<LeadSource>())}.");

        RuleFor(x => x.InterestedPlotId)
            .Must(id => ObjectId.TryParse(id, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.InterestedPlotId))
            .WithMessage("InterestedPlotId must be a valid identifier.");

        RuleFor(x => x.AssignedEmployeeId)
            .NotEmpty().WithMessage("AssignedEmployeeId is required.")
            .Must(id => ObjectId.TryParse(id, out _)).WithMessage("AssignedEmployeeId must be a valid identifier.");
    }
}
