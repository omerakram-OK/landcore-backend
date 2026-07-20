using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Validators;

public class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequestDto>
{
    public RecordPaymentRequestValidator()
    {
        RuleFor(x => x.InstallmentPlanId)
            .NotEmpty().WithMessage("InstallmentPlanId is required.")
            .Must(id => ObjectId.TryParse(id, out _)).WithMessage("InstallmentPlanId must be a valid identifier.");

        RuleFor(x => x.InstallmentSeqNo)
            .GreaterThan(0).WithMessage("InstallmentSeqNo must be greater than zero.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Mode)
            .NotEmpty().WithMessage("Mode is required.")
            .Must(mode => Enum.TryParse<PaymentMode>(mode, ignoreCase: true, out _))
            .WithMessage($"Mode must be one of: {string.Join(", ", Enum.GetNames<PaymentMode>())}.");

        RuleFor(x => x.BankAccountId)
            .Must(id => ObjectId.TryParse(id, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.BankAccountId))
            .WithMessage("BankAccountId must be a valid identifier.");

        RuleFor(x => x.BankAccountId)
            .NotEmpty()
            .When(x => Enum.TryParse<PaymentMode>(x.Mode, ignoreCase: true, out var m) && m is PaymentMode.Bank or PaymentMode.Cheque)
            .WithMessage("BankAccountId is required for Bank/Cheque payment modes.");

        RuleFor(x => x.Date)
            .NotEqual(default(DateTime)).WithMessage("Date is required.");

        When(x => Enum.TryParse<PaymentMode>(x.Mode, ignoreCase: true, out var m) && m == PaymentMode.Cheque, () =>
        {
            RuleFor(x => x.ChequeNumber)
                .NotEmpty().WithMessage("ChequeNumber is required when Mode is Cheque.");

            RuleFor(x => x.ChequeBank)
                .NotEmpty().WithMessage("ChequeBank is required when Mode is Cheque.");

            RuleFor(x => x.ChequeDueDate)
                .NotNull().WithMessage("ChequeDueDate is required when Mode is Cheque.");

            RuleFor(x => x.ChequeDepositDate)
                .NotNull().WithMessage("ChequeDepositDate is required when Mode is Cheque.");
        });
    }
}
