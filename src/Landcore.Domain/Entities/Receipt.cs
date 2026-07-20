using Landcore.Domain.Common;
using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class Receipt : TenantEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;

    public ObjectId PaymentId { get; set; }
}
