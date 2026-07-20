using Landcore.Domain.Common;

namespace Landcore.Domain.Entities;

public class Society : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int TotalPlots { get; set; }
}
