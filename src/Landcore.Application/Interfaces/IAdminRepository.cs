using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface IAdminRepository
{
    Task<Admin?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default);

    Task<Admin?> GetByContactEmailAsync(string contactEmail, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Admin>> GetAllAsync(CancellationToken cancellationToken = default);

    Task CreateAsync(Admin admin, CancellationToken cancellationToken = default);

    Task UpdateAsync(Admin admin, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(ObjectId id, AdminStatus status, ObjectId updatedBy, CancellationToken cancellationToken = default);

    Task SetSubscriptionIdAsync(ObjectId adminId, ObjectId subscriptionId, ObjectId updatedBy, CancellationToken cancellationToken = default);

    Task SetLogoAsync(ObjectId adminId, string? logoBase64, string? logoContentType, ObjectId updatedBy, CancellationToken cancellationToken = default);
}
