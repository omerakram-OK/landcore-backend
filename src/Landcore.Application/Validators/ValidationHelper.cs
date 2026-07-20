using FluentValidation;
using Landcore.Application.Exceptions;

namespace Landcore.Application.Validators;

public static class ValidationHelper
{
    public static async Task ValidateOrThrowAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (result.IsValid)
        {
            return;
        }

        var details = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                fieldGroup => fieldGroup.Key,
                fieldGroup => (object)fieldGroup.Select(e => e.ErrorMessage).ToArray());

        throw new ValidationAppException("One or more fields are invalid.", details);
    }
}
