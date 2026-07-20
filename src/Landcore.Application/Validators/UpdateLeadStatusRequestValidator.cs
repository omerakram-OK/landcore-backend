using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Domain.Enums;

namespace Landcore.Application.Validators;

public class UpdateLeadStatusRequestValidator : AbstractValidator<UpdateLeadStatusRequestDto>
{
    public UpdateLeadStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(status => Enum.TryParse<LeadStatus>(status, ignoreCase: true, out _))
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<LeadStatus>())}.");
    }
}
