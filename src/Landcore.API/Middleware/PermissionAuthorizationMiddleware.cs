using System.Security.Claims;
using System.Text.Json;
using Landcore.Common;

namespace Landcore.API.Middleware;

public class PermissionAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public PermissionAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var requirement = endpoint?.Metadata.GetMetadata<RequirePermissionAttribute>();

        if (requirement is null)
        {
            await _next(context);
            return;
        }

        var user = context.User;
        if (user.Identity is not { IsAuthenticated: true })
        {
            await WriteForbiddenAsync(context, "Authentication is required.");
            return;
        }

        var role = user.FindFirst(ClaimTypes.Role)?.Value;

        if (string.Equals(role, Constants.Roles.SuperMan, StringComparison.Ordinal))
        {
            await _next(context);
            return;
        }

        if (string.Equals(role, Constants.Roles.Admin, StringComparison.Ordinal))
        {
            await _next(context);
            return;
        }

        if (string.Equals(role, Constants.Roles.Employee, StringComparison.Ordinal))
        {
            var requiredPermission = $"{requirement.Module}:{requirement.Action}";
            var granted = user.Claims.Any(claim =>
                claim.Type == Constants.ClaimTypes.Permission &&
                string.Equals(claim.Value, requiredPermission, StringComparison.Ordinal));

            if (granted)
            {
                await _next(context);
                return;
            }

            await WriteForbiddenAsync(
                context,
                $"Your Designation does not grant '{requirement.Action}' on '{requirement.Module}'.");
            return;
        }

        await WriteForbiddenAsync(context, "Unrecognized role.");
    }

    private static async Task WriteForbiddenAsync(HttpContext context, string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            success = false,
            error = new { code = "FORBIDDEN", message },
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions), context.RequestAborted);
    }
}
