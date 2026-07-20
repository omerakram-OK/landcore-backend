using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class LeadRepository : ILeadRepository
{
    private readonly MongoDbContext _context;

    public LeadRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Lead?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Lead>.Filter.Eq(x => x.Id, id)
                     & Builders<Lead>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Lead>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Leads.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Lead>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Lead>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Lead>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Leads.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        await _context.Leads.InsertOneAsync(lead, options: null, cancellationToken);
    }

    public async Task UpdateAsync(Lead lead, CancellationToken cancellationToken = default)
    {
        await _context.Leads.ReplaceOneAsync(x => x.Id == lead.Id, lead, cancellationToken: cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(ObjectId adminId, ObjectId id, ObjectId deletedBy, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Lead>.Filter.Eq(x => x.Id, id)
                     & Builders<Lead>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Lead>.Filter.Eq(x => x.IsDeleted, false);

        var update = Builders<Lead>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.DeletedAt, DateTime.UtcNow)
            .Set(x => x.DeletedBy, deletedBy)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedBy, deletedBy);

        var result = await _context.Leads.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }
}
