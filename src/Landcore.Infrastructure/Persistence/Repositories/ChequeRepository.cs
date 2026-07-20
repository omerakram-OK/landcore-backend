using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class ChequeRepository : IChequeRepository
{
    private readonly MongoDbContext _context;

    public ChequeRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Cheque?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Cheque>.Filter.Eq(x => x.Id, id)
                     & Builders<Cheque>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Cheque>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Cheques.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Cheque?> GetByPaymentIdAsync(ObjectId adminId, ObjectId paymentId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Cheque>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Cheque>.Filter.Eq(x => x.PaymentId, paymentId)
                     & Builders<Cheque>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Cheques.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Cheque>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Cheque>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Cheque>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Cheques.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Cheque cheque, CancellationToken cancellationToken = default)
    {
        await _context.Cheques.InsertOneAsync(cheque, options: null, cancellationToken);
    }

    public async Task UpdateAsync(Cheque cheque, CancellationToken cancellationToken = default)
    {
        await _context.Cheques.ReplaceOneAsync(x => x.Id == cheque.Id, cheque, cancellationToken: cancellationToken);
    }
}
