using FluentValidation;
using Landcore.Application.DTOs;
using MongoDB.Bson;

namespace Landcore.Application.Validators;

public class CreateClientRequestValidator : AbstractValidator<CreateClientRequestDto>
{
    public CreateClientRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200);

        RuleFor(x => x.CNIC)
            .NotEmpty().WithMessage("CNIC is required.")
            .MustBeValidCnic();

        RuleFor(x => x.Phones)
            .NotNull().WithMessage("Phones is required.")
            .Must(phones => phones.Count > 0).WithMessage("At least one phone number is required.");

        RuleForEach(x => x.Phones)
            .NotEmpty().WithMessage("Phone must not be empty.")
            .MustBeValidPakistaniPhone();

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500);

        RuleFor(x => x.EmergencyContact)
            .MaximumLength(300);

        RuleFor(x => x.LinkedAgentId)
            .Must(id => ObjectId.TryParse(id, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.LinkedAgentId))
            .WithMessage("LinkedAgentId must be a valid identifier.");

        RuleForEach(x => x.CoOwnerClientIds)
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Each CoOwnerClientId must be a valid identifier.");
    }
}
