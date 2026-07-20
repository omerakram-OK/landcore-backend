namespace Landcore.API.Middleware;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class RequirePermissionAttribute : Attribute
{
    public RequirePermissionAttribute(string module, string action)
    {
        Module = module;
        Action = action;
    }

    public string Module { get; }

    public string Action { get; }
}
