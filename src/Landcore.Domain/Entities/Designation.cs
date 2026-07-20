using Landcore.Domain.Common;

namespace Landcore.Domain.Entities;

public class Designation : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public List<Permission> Permissions { get; set; } = new();
}
