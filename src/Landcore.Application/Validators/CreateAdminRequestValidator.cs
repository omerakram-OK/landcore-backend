using FluentValidation;
using Landcore.Application.DTOs;

namespace Landcore.Application.Validators;

public class CreateAdminRequestValidator : AbstractValidator<CreateAdminRequestDto>
{
    public CreateAdminRequestValidator()
    {
        RuleFor(x => x.SocietyName)
            .NotEmpty().WithMessage("Society name is required.")
            .MaximumLength(200);

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Contact email is required.")
            .EmailAddress().WithMessage("Contact email must be a valid email address.");

        RuleFor(x => x.InitialPassword)
            .NotEmpty().WithMessage("Initial password is required.")
            .MinimumLength(8).WithMessage("Initial password must be at least 8 characters.");
    }
}
