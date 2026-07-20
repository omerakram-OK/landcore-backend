using MongoDB.Bson;

namespace Landcore.Domain.Common;

public abstract class TenantEntity : BaseEntity
{
    public ObjectId AdminId { get; set; }
}
