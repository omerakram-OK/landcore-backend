using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly MongoDbContext _context;

    public ClientRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Client?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Client>.Filter.Eq(x => x.Id, id)
                     & Builders<Client>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Client>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Clients.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Client>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Client>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Client>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Clients.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Client client, CancellationToken cancellationToken = default)
    {
        await _context.Clients.InsertOneAsync(client, options: null, cancellationToken);
    }

    public async Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        await _context.Clients.ReplaceOneAsync(x => x.Id == client.Id, client, cancellationToken: cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(ObjectId adminId, ObjectId id, ObjectId deletedBy, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Client>.Filter.Eq(x => x.Id, id)
                     & Builders<Client>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Client>.Filter.Eq(x => x.IsDeleted, false);

        var update = Builders<Client>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.DeletedAt, DateTime.UtcNow)
            .Set(x => x.DeletedBy, deletedBy)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedBy, deletedBy);

        var result = await _context.Clients.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }
}
