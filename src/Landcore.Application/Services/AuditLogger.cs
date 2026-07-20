using Landcore.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Landcore.Application.Services;

public class AuditLogger : IAuditLogger
{
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(ILogger<AuditLogger> logger)
    {
        _logger = logger;
    }

    public void LogAction(string userId, string action, string entity, string entityId, string? adminScope, object? details = null)
    {
        _logger.LogInformation(
            "AUDIT {UtcTimestamp:o} User={UserId} Action={Action} Entity={Entity} EntityId={EntityId} AdminScope={AdminScope} Details={@Details}",
            DateTime.UtcNow,
            userId,
            action,
            entity,
            entityId,
            adminScope ?? "(platform)",
            details);
    }
}
