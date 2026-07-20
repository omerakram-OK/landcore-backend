using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class RefundRecordRepository : IRefundRecordRepository
{
    private readonly MongoDbContext _context;

    public RefundRecordRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<RefundRecord?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RefundRecord>.Filter.Eq(x => x.Id, id)
                     & Builders<RefundRecord>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<RefundRecord>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.RefundRecords.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RefundRecord>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RefundRecord>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<RefundRecord>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.RefundRecords.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RefundRecord>> GetByPlotIdAsync(ObjectId adminId, ObjectId plotId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RefundRecord>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<RefundRecord>.Filter.Eq(x => x.PlotId, plotId)
                     & Builders<RefundRecord>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.RefundRecords.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(RefundRecord record, CancellationToken cancellationToken = default)
    {
        await _context.RefundRecords.InsertOneAsync(record, options: null, cancellationToken);
    }

    public async Task UpdateAsync(RefundRecord record, CancellationToken cancellationToken = default)
    {
        await _context.RefundRecords.ReplaceOneAsync(x => x.Id == record.Id, record, cancellationToken: cancellationToken);
    }
}
