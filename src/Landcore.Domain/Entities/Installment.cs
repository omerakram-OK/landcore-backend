using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class Installment
{
    public int SeqNo { get; set; }

    public DateTime DueDate { get; set; }

    public Decimal128 Amount { get; set; }

    public InstallmentStatus Status { get; set; }

    public Decimal128 PaidAmount { get; set; }
}
