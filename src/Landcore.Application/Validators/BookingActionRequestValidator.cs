using FluentValidation;
using Landcore.Application.DTOs;

namespace Landcore.Application.Validators;

public class BookingActionRequestValidator : AbstractValidator<BookingActionRequestDto>
{
    public BookingActionRequestValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }
}
