using Landcore.Domain.Common;
using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class InstallmentPlan : TenantEntity
{
    public ObjectId BookingId { get; set; }

    public Decimal128 DownPayment { get; set; }

    public Decimal128? EarlyPaymentDiscount { get; set; }

    public List<Installment> Installments { get; set; } = new();

    public Decimal128 CreditBalance { get; set; }
}
