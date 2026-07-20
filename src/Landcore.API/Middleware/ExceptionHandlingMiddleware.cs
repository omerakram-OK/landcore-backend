using System.Text.Json;
using System.Text.Json.Serialization;
using Landcore.Application.Exceptions;

namespace Landcore.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (statusCode, code, message, details) = Map(exception);

        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled exception processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                "Request {Method} {Path} failed with {Code}: {Message}",
                context.Request.Method,
                context.Request.Path,
                code,
                exception.Message);
        }

        if (context.Response.HasStarted)
        {
            throw exception;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            success = false,
            error = new { code, message, details },
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions), context.RequestAborted);
    }

    private static (int StatusCode, string Code, string Message, object? Details) Map(Exception exception) =>
        exception switch
        {
            AppException appException => (appException.StatusCode, appException.Code, appException.Message, appException.Details),
            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred. Please try again later.", null),
        };
}
