using Landcore.Domain.Common;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class RefundRecord : TenantEntity
{
    public ObjectId PlotId { get; set; }

    public ObjectId BookingId { get; set; }

    public ObjectId InstallmentPlanId { get; set; }

    public ObjectId ClientId { get; set; }

    public Decimal128 AmountPaid { get; set; }

    public Decimal128 CompanyProfitAmount { get; set; }

    public Decimal128 ClientRefundAmount { get; set; }

    public DateTime PaymentDate { get; set; }

    public RefundRecordStatus Status { get; set; }

    public DateTime? IssuedAt { get; set; }

    public ObjectId? IssuedBy { get; set; }

    public string? Notes { get; set; }
}
