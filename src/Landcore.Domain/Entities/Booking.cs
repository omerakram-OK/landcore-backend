using Landcore.Domain.Common;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class Booking : TenantEntity
{
    public ObjectId PlotId { get; set; }

    public ObjectId ClientId { get; set; }

    public ObjectId? LeadId { get; set; }

    public ObjectId? AgentId { get; set; }

    public BookingCommissionSnapshot? CommissionSnapshot { get; set; }

    public Decimal128 TokenAmount { get; set; }

    public DateTime ExpiryDate { get; set; }

    public BookingStatus Status { get; set; }

    public class BookingCommissionSnapshot
    {
        public CommissionType Type { get; set; }

        public Decimal128 Value { get; set; }
    }
}
