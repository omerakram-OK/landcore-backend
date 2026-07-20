using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class AgentRepository : IAgentRepository
{
    private readonly MongoDbContext _context;

    public AgentRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Agent?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Agent>.Filter.Eq(x => x.Id, id)
                     & Builders<Agent>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Agent>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Agents.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Agent>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Agent>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Agent>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Agents.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        await _context.Agents.InsertOneAsync(agent, options: null, cancellationToken);
    }

    public async Task UpdateAsync(Agent agent, CancellationToken cancellationToken = default)
    {
        await _context.Agents.ReplaceOneAsync(x => x.Id == agent.Id, agent, cancellationToken: cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(ObjectId adminId, ObjectId id, ObjectId deletedBy, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Agent>.Filter.Eq(x => x.Id, id)
                     & Builders<Agent>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Agent>.Filter.Eq(x => x.IsDeleted, false);

        var update = Builders<Agent>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.DeletedAt, DateTime.UtcNow)
            .Set(x => x.DeletedBy, deletedBy)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedBy, deletedBy);

        var result = await _context.Agents.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }
}
