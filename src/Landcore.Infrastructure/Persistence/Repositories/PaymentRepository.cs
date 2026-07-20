using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly MongoDbContext _context;

    public PaymentRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Payment>.Filter.Eq(x => x.Id, id)
                     & Builders<Payment>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Payment>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Payments.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Payment>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Payment>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Payments.Find(filter).SortByDescending(x => x.Date).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetByInstallmentPlanIdAsync(ObjectId adminId, ObjectId installmentPlanId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Payment>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Payment>.Filter.Eq(x => x.InstallmentPlanId, installmentPlanId)
                     & Builders<Payment>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Payments.Find(filter).SortByDescending(x => x.Date).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _context.Payments.InsertOneAsync(payment, options: null, cancellationToken);
    }

    public async Task<decimal> SumAmountAcrossAllAdminsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Payment>.Filter.Gte(x => x.Date, from)
                     & Builders<Payment>.Filter.Lte(x => x.Date, to)
                     & Builders<Payment>.Filter.Eq(x => x.IsDeleted, false);
        var payments = await _context.Payments.Find(filter).ToListAsync(cancellationToken);
        return payments.Sum(payment => (decimal)payment.Amount);
    }
}
