using Landcore.Domain.Common;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class Subscription : TenantEntity
{
    public SubscriptionPlan Plan { get; set; }

    public Decimal128 FeeAmount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime NextDueDate { get; set; }

    public SubscriptionStatus Status { get; set; }
}
