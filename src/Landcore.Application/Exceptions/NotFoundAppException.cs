namespace Landcore.Application.Exceptions;

public sealed class NotFoundAppException : AppException
{
    public NotFoundAppException(string message)
        : base("NOT_FOUND", message, 404)
    {
    }
}
