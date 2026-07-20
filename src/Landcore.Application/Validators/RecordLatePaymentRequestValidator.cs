using FluentValidation;
using Landcore.Application.DTOs;

namespace Landcore.Application.Validators;

public class RecordLatePaymentRequestValidator : AbstractValidator<RecordLatePaymentRequestDto>
{
    public RecordLatePaymentRequestValidator()
    {
        RuleFor(x => x.AmountPaid)
            .GreaterThan(0).WithMessage("AmountPaid must be greater than 0.");

        RuleFor(x => x.PaymentDate)
            .NotEqual(default(DateTime)).WithMessage("PaymentDate is required.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }
}
