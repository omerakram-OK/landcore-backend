using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface ILeadRepository
{
    Task<Lead?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Lead>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);

    Task CreateAsync(Lead lead, CancellationToken cancellationToken = default);

    Task UpdateAsync(Lead lead, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(ObjectId adminId, ObjectId id, ObjectId deletedBy, CancellationToken cancellationToken = default);
}
