using FluentValidation;
using Landcore.Application.DTOs;

namespace Landcore.Application.Validators;

public class UpdateBankAccountRequestValidator : AbstractValidator<UpdateBankAccountRequestDto>
{
    public UpdateBankAccountRequestValidator()
    {
        RuleFor(x => x.AccountName)
            .NotEmpty().WithMessage("Account name/title is required.")
            .MaximumLength(200);

        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Account number is required.")
            .MaximumLength(50);

        RuleFor(x => x.BankName)
            .NotEmpty().WithMessage("Bank name is required.")
            .MaximumLength(200);
    }
}
