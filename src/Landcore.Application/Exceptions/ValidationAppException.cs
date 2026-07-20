namespace Landcore.Application.Exceptions;

public sealed class ValidationAppException : AppException
{
    public ValidationAppException(string message, object? details = null)
        : base("VALIDATION_ERROR", message, 400, details)
    {
    }
}
