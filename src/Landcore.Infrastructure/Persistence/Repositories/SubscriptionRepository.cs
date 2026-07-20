using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly MongoDbContext _context;

    public SubscriptionRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Subscription?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Subscription>.Filter.Eq(x => x.Id, id)
                     & Builders<Subscription>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Subscriptions.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Subscription?> GetByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Subscription>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Subscription>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Subscriptions.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Subscription>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Subscriptions.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Subscriptions.InsertOneAsync(subscription, options: null, cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new ValidationAppException(
                "This Admin already has a Subscription. Use update/activate/suspend/reactivate instead of creating a new one.",
                new Dictionary<string, string[]> { ["AdminId"] = ["This Admin already has a Subscription."] });
        }
    }

    public async Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        await _context.Subscriptions.ReplaceOneAsync(x => x.Id == subscription.Id, subscription, cancellationToken: cancellationToken);
    }

    public async Task UpdateStatusAsync(ObjectId id, SubscriptionStatus status, ObjectId updatedBy, CancellationToken cancellationToken = default)
    {
        var update = Builders<Subscription>.Update
            .Set(x => x.Status, status)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedBy, updatedBy);

        await _context.Subscriptions.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
    }
}
