using Landcore.Domain.Common;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class Payment : TenantEntity
{
    public ObjectId InstallmentPlanId { get; set; }

    public int InstallmentSeqNo { get; set; }

    public Decimal128 Amount { get; set; }

    public PaymentMode Mode { get; set; }

    public ObjectId? BankAccountId { get; set; }

    public DateTime Date { get; set; }

    public ObjectId ReceiptId { get; set; }

    public Decimal128? CreditBalanceApplied { get; set; }

    public Decimal128 AmountAppliedToInstallment { get; set; }
}
