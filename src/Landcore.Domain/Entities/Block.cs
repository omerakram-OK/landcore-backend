using Landcore.Domain.Common;
using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class Block : TenantEntity
{
    public ObjectId SocietyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int TotalPlots { get; set; }
}
