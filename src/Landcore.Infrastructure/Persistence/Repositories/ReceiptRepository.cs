using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class ReceiptRepository : IReceiptRepository
{
    private readonly MongoDbContext _context;

    public ReceiptRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Receipt?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Receipt>.Filter.Eq(x => x.Id, id)
                     & Builders<Receipt>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Receipt>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Receipts.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Receipt?> GetByPaymentIdAsync(ObjectId adminId, ObjectId paymentId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Receipt>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Receipt>.Filter.Eq(x => x.PaymentId, paymentId)
                     & Builders<Receipt>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Receipts.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Receipt>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Receipt>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Receipt>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Receipts.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        await _context.Receipts.InsertOneAsync(receipt, options: null, cancellationToken);
    }

    public async Task<long> CountByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Receipt>.Filter.Eq(x => x.AdminId, adminId);
        return await _context.Receipts.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }
}
