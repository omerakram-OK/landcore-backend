using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class BankAccountRepository : IBankAccountRepository
{
    private readonly MongoDbContext _context;

    public BankAccountRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<BankAccount?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BankAccount>.Filter.Eq(x => x.Id, id)
                     & Builders<BankAccount>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<BankAccount>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.BankAccounts.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BankAccount>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BankAccount>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<BankAccount>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.BankAccounts.Find(filter).SortBy(x => x.AccountName).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(BankAccount bankAccount, CancellationToken cancellationToken = default)
    {
        await _context.BankAccounts.InsertOneAsync(bankAccount, options: null, cancellationToken);
    }

    public async Task UpdateAsync(BankAccount bankAccount, CancellationToken cancellationToken = default)
    {
        await _context.BankAccounts.ReplaceOneAsync(x => x.Id == bankAccount.Id, bankAccount, cancellationToken: cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(ObjectId adminId, ObjectId id, ObjectId deletedBy, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BankAccount>.Filter.Eq(x => x.Id, id)
                     & Builders<BankAccount>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<BankAccount>.Filter.Eq(x => x.IsDeleted, false);

        var update = Builders<BankAccount>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.DeletedAt, DateTime.UtcNow)
            .Set(x => x.DeletedBy, deletedBy)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedBy, deletedBy);

        var result = await _context.BankAccounts.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }
}
