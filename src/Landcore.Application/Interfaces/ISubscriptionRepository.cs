using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default);

    Task<Subscription?> GetByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Subscription>> GetAllAsync(CancellationToken cancellationToken = default);

    Task CreateAsync(Subscription subscription, CancellationToken cancellationToken = default);

    Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(ObjectId id, SubscriptionStatus status, ObjectId updatedBy, CancellationToken cancellationToken = default);
}
