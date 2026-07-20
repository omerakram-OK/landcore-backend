using Landcore.Domain.Common;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class GeneratedDocument : TenantEntity
{
    public ObjectId PlotId { get; set; }

    public ObjectId ClientId { get; set; }

    public ObjectId? BookingId { get; set; }

    public DocumentType DocumentType { get; set; }

    public string FileUrl { get; set; } = string.Empty;

    public byte[] FileContent { get; set; } = Array.Empty<byte>();

    public DateTime GeneratedAt { get; set; }
}
