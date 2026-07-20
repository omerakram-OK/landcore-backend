using Landcore.Domain.Common;
using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class Employee : TenantEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public ObjectId DesignationId { get; set; }
}
