using Landcore.Domain.Common;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class Plot : TenantEntity
{
    public string PlotNumber { get; set; } = string.Empty;

    public ObjectId BlockId { get; set; }

    public ObjectId SocietyId { get; set; }

    public decimal Size { get; set; }

    public PlotSizeUnit SizeUnit { get; set; }

    public PlotCategory Category { get; set; }

    public Decimal128 BasePrice { get; set; }

    public List<PlotCharge> Charges { get; set; } = new();

    public Decimal128 AnnualMaintenanceCharge { get; set; }

    public PlotStatus Status { get; set; }

    public PossessionStatus PossessionStatus { get; set; }

    public List<ObjectId> OwnerClientIds { get; set; } = new();

    public List<HistoryLogEntry> HistoryLog { get; set; } = new();

    public class HistoryLogEntry
    {
        public string Event { get; set; } = string.Empty;

        public string Details { get; set; } = string.Empty;

        public DateTime At { get; set; }

        public ObjectId By { get; set; }
    }
}
