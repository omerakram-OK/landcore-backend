namespace Landcore.Application.Exceptions;

public sealed class AuthenticationFailedException : AppException
{
    public AuthenticationFailedException(string message)
        : base("INVALID_CREDENTIALS", message, 401)
    {
    }
}
