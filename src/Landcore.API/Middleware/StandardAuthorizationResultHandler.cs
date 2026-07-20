using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Landcore.API.Middleware;

public class StandardAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private static readonly AuthorizationMiddlewareResultHandler DefaultHandler = new();
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        await DefaultHandler.HandleAsync(next, context, policy, authorizeResult);

        if (authorizeResult.Succeeded || context.Response.HasStarted)
        {
            return;
        }

        var statusCode = context.Response.StatusCode;
        if (statusCode != StatusCodes.Status401Unauthorized && statusCode != StatusCodes.Status403Forbidden)
        {
            return;
        }

        var (code, message) = statusCode == StatusCodes.Status403Forbidden
            ? ("FORBIDDEN", "You do not have permission to perform this action.")
            : ("UNAUTHORIZED", "Authentication is required.");

        context.Response.ContentType = "application/json";
        var payload = new { success = false, error = new { code, message } };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions), context.RequestAborted);
    }
}
