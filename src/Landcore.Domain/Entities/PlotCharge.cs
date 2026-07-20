using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class PlotCharge
{
    public string ChargeType { get; set; } = string.Empty;

    public Decimal128 Amount { get; set; }
}
