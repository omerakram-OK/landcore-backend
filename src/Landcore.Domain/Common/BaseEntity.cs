using MongoDB.Bson;

namespace Landcore.Domain.Common;

public abstract class BaseEntity
{
    public ObjectId Id { get; set; }

    public DateTime CreatedAt { get; set; }
    public ObjectId CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }
    public ObjectId UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public ObjectId? DeletedBy { get; set; }
}
