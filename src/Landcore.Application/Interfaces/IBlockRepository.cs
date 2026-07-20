using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface IBlockRepository
{
    Task<Block?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default);

    Task<Block?> GetByNameAsync(ObjectId adminId, ObjectId societyId, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Block>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Block>> GetAllBySocietyIdAsync(ObjectId adminId, ObjectId societyId, CancellationToken cancellationToken = default);

    Task CreateAsync(Block block, CancellationToken cancellationToken = default);

    Task UpdateAsync(Block block, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(ObjectId adminId, ObjectId id, ObjectId deletedBy, CancellationToken cancellationToken = default);
}
