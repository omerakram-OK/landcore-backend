using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface ISocietyRepository
{
    Task<Society?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default);

    Task<Society?> GetByNameAsync(ObjectId adminId, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Society>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);

    Task CreateAsync(Society society, CancellationToken cancellationToken = default);

    Task UpdateAsync(Society society, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(ObjectId adminId, ObjectId id, ObjectId deletedBy, CancellationToken cancellationToken = default);
}
