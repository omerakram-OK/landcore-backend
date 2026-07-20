using Landcore.Domain.Common;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class Agent : TenantEntity
{
    public string FullName { get; set; } = string.Empty;

    public string CNIC { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public CommissionType CommissionType { get; set; }

    public Decimal128 CommissionValue { get; set; }
}
