using Landcore.Domain.Common;
using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class Client : TenantEntity
{
    public string FullName { get; set; } = string.Empty;

    public string CNIC { get; set; } = string.Empty;

    public List<string> Phones { get; set; } = new();

    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string EmergencyContact { get; set; } = string.Empty;

    public ObjectId? LinkedAgentId { get; set; }

    public List<ObjectId> CoOwnerClientIds { get; set; } = new();
}
