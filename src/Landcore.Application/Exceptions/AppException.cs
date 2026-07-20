namespace Landcore.Application.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string code, string message, int statusCode, object? details = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        Details = details;
    }

    public string Code { get; }

    public int StatusCode { get; }

    public object? Details { get; }
}
