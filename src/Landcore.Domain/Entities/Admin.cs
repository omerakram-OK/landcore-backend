using Landcore.Domain.Common;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Domain.Entities;

public class Admin : BaseEntity
{
    public string SocietyName { get; set; } = string.Empty;

    public string ContactEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public ObjectId SubscriptionId { get; set; }

    public AdminStatus Status { get; set; }

    public string? LogoBase64 { get; set; }

    public string? LogoContentType { get; set; }
}
