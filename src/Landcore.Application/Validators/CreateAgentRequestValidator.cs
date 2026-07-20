using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Domain.Enums;

namespace Landcore.Application.Validators;

public class CreateAgentRequestValidator : AbstractValidator<CreateAgentRequestDto>
{
    public CreateAgentRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200);

        RuleFor(x => x.CNIC)
            .NotEmpty().WithMessage("CNIC is required.")
            .MustBeValidCnic();

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .MustBeValidPakistaniPhone();

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500);

        RuleFor(x => x.CommissionType)
            .NotEmpty().WithMessage("CommissionType is required.")
            .Must(type => Enum.TryParse<CommissionType>(type, ignoreCase: true, out _))
            .WithMessage($"CommissionType must be one of: {string.Join(", ", Enum.GetNames<CommissionType>())}.");

        RuleFor(x => x.CommissionValue)
            .GreaterThanOrEqualTo(0).WithMessage("CommissionValue must be zero or greater.");

        RuleFor(x => x.CommissionValue)
            .LessThanOrEqualTo(100)
            .When(x => Enum.TryParse<CommissionType>(x.CommissionType, ignoreCase: true, out var t) && t == CommissionType.Percentage)
            .WithMessage("CommissionValue must be between 0 and 100 when CommissionType is Percentage.");
    }
}
