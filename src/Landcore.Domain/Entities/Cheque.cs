using Landcore.Domain.Common;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class Cheque : TenantEntity
{
    public ObjectId PaymentId { get; set; }

    public string ChequeNumber { get; set; } = string.Empty;

    public string Bank { get; set; } = string.Empty;

    public Decimal128 Amount { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime DepositDate { get; set; }

    public ChequeStatus Status { get; set; }

    public Decimal128? BouncePenaltyAmount { get; set; }
}
