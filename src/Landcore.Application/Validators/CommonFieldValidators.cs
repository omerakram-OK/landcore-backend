using FluentValidation;
using Landcore.Common.Helpers;

namespace Landcore.Application.Validators;

public static class CommonFieldValidators
{
    public static IRuleBuilderOptions<T, string> MustBeValidCnic<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .Matches(RegexPatterns.Cnic)
            .WithMessage("CNIC must be in the format 12345-1234567-1.");

    public static IRuleBuilderOptions<T, string> MustBeValidPakistaniPhone<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .Matches(RegexPatterns.Phone)
            .WithMessage("Phone must be a valid Pakistani number, e.g. 03001234567 or +923001234567.");
}
