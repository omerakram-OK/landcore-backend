namespace Landcore.Application.Interfaces;

public interface IAuditLogger
{
    void LogAction(string userId, string action, string entity, string entityId, string? adminScope, object? details = null);
}
