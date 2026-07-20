using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface IDesignationRepository
{
    Task<Designation?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default);

    Task<Designation?> GetByNameAsync(ObjectId adminId, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Designation>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);

    Task CreateAsync(Designation designation, CancellationToken cancellationToken = default);

    Task UpdateAsync(Designation designation, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(ObjectId adminId, ObjectId id, ObjectId deletedBy, CancellationToken cancellationToken = default);
}
