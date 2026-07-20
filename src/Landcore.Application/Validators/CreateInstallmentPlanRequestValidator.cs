using FluentValidation;
using Landcore.Application.DTOs;
using MongoDB.Bson;

namespace Landcore.Application.Validators;

public class CreateInstallmentPlanRequestValidator : AbstractValidator<CreateInstallmentPlanRequestDto>
{
    public CreateInstallmentPlanRequestValidator()
    {
        RuleFor(x => x.BookingId)
            .NotEmpty().WithMessage("BookingId is required.")
            .Must(id => ObjectId.TryParse(id, out _)).WithMessage("BookingId must be a valid identifier.");

        RuleFor(x => x.DownPayment)
            .GreaterThanOrEqualTo(0).WithMessage("DownPayment must be zero or greater.");

        RuleFor(x => x.EarlyPaymentDiscount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.EarlyPaymentDiscount.HasValue)
            .WithMessage("EarlyPaymentDiscount must be zero or greater.");

        RuleFor(x => x.Installments)
            .NotEmpty().WithMessage("At least one installment is required.");

        RuleForEach(x => x.Installments).ChildRules(installment =>
        {
            installment.RuleFor(i => i.Amount)
                .GreaterThan(0).WithMessage("Installment Amount must be greater than zero.");

            installment.RuleFor(i => i.DueDate)
                .NotEqual(default(DateTime)).WithMessage("Installment DueDate is required.");
        });
    }
}
