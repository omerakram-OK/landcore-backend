using FluentValidation;
using Landcore.Application.DTOs;

namespace Landcore.Application.Validators;

public class UpdateAdminRequestValidator : AbstractValidator<UpdateAdminRequestDto>
{
    public UpdateAdminRequestValidator()
    {
        RuleFor(x => x.SocietyName)
            .NotEmpty().WithMessage("Society name is required.")
            .MaximumLength(200);

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Contact email is required.")
            .EmailAddress().WithMessage("Contact email must be a valid email address.");
    }
}
