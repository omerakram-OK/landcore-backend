using FluentValidation;
using Landcore.Application.DTOs;

namespace Landcore.Application.Validators;

public class CreateDesignationRequestValidator : AbstractValidator<CreateDesignationRequestDto>
{
    public CreateDesignationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Designation name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Permissions)
            .NotNull().WithMessage("Permissions is required (an empty list is allowed for a no-access Designation).");

        RuleForEach(x => x.Permissions).ChildRules(permissionRules =>
        {
            permissionRules.RuleFor(permission => permission.Module)
                .NotEmpty().WithMessage("Each permission entry must have a Module.");

            permissionRules.RuleFor(permission => permission.Actions)
                .NotNull().WithMessage("Each permission entry must have an Actions list.")
                .Must(actions => actions.Count > 0).WithMessage("Each permission entry must grant at least one Action.");

            permissionRules.RuleForEach(permission => permission.Actions)
                .NotEmpty().WithMessage("Action names must not be empty.");
        });
    }
}
