using Landcore.Domain.Common;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class ApprovalRequest : TenantEntity
{
    public ApprovalRequestType Type { get; set; }

    public ObjectId RequestedByEmployeeId { get; set; }

    public string Justification { get; set; } = string.Empty;

    public ApprovalRequestStatus Status { get; set; }

    public ObjectId? DecidedByAdminId { get; set; }

    public string? DecisionNotes { get; set; }

    public ObjectId? TargetPlotId { get; set; }

    public string? PayloadJson { get; set; }
}
