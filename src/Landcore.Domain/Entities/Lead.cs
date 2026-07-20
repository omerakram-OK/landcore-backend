using Landcore.Domain.Common;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class Lead : TenantEntity
{
    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public LeadSource Source { get; set; }

    public ObjectId? InterestedPlotId { get; set; }

    public LeadStatus Status { get; set; }

    public ObjectId AssignedEmployeeId { get; set; }

    public List<FollowUpNote> FollowUpNotes { get; set; } = new();

    public class FollowUpNote
    {
        public string Note { get; set; } = string.Empty;

        public ObjectId By { get; set; }

        public DateTime At { get; set; }
    }
}
