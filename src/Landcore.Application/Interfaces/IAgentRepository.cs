using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface IAgentRepository
{
    Task<Agent?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Agent>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);

    Task CreateAsync(Agent agent, CancellationToken cancellationToken = default);

    Task UpdateAsync(Agent agent, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(ObjectId adminId, ObjectId id, ObjectId deletedBy, CancellationToken cancellationToken = default);
}
