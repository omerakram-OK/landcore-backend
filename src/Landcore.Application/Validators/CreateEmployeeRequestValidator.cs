using FluentValidation;
using Landcore.Application.DTOs;
using MongoDB.Bson;

namespace Landcore.Application.Validators;

public class CreateEmployeeRequestValidator : AbstractValidator<CreateEmployeeRequestDto>
{
    public CreateEmployeeRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required.")
            .MustBeValidPakistaniPhone();

        RuleFor(x => x.InitialPassword)
            .NotEmpty().WithMessage("Initial password is required.")
            .MinimumLength(8).WithMessage("Initial password must be at least 8 characters.");

        RuleFor(x => x.DesignationId)
            .NotEmpty().WithMessage("DesignationId is required.")
            .Must(id => ObjectId.TryParse(id, out _)).WithMessage("DesignationId must be a valid identifier.");
    }
}
