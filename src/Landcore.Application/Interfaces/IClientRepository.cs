using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Client>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);

    Task CreateAsync(Client client, CancellationToken cancellationToken = default);

    Task UpdateAsync(Client client, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(ObjectId adminId, ObjectId id, ObjectId deletedBy, CancellationToken cancellationToken = default);
}
