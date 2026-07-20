using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class InstallmentPlanRepository : IInstallmentPlanRepository
{
    private readonly MongoDbContext _context;

    public InstallmentPlanRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<InstallmentPlan?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<InstallmentPlan>.Filter.Eq(x => x.Id, id)
                     & Builders<InstallmentPlan>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<InstallmentPlan>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.InstallmentPlans.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<InstallmentPlan?> GetByBookingIdAsync(ObjectId adminId, ObjectId bookingId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<InstallmentPlan>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<InstallmentPlan>.Filter.Eq(x => x.BookingId, bookingId)
                     & Builders<InstallmentPlan>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.InstallmentPlans.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InstallmentPlan>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<InstallmentPlan>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<InstallmentPlan>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.InstallmentPlans.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(InstallmentPlan plan, CancellationToken cancellationToken = default)
    {
        await _context.InstallmentPlans.InsertOneAsync(plan, options: null, cancellationToken);
    }

    public async Task UpdateAsync(InstallmentPlan plan, CancellationToken cancellationToken = default)
    {
        await _context.InstallmentPlans.ReplaceOneAsync(x => x.Id == plan.Id, plan, cancellationToken: cancellationToken);
    }
}
