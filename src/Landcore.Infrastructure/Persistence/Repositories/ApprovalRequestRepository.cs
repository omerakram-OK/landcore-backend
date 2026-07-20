using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class ApprovalRequestRepository : IApprovalRequestRepository
{
    private readonly MongoDbContext _context;

    public ApprovalRequestRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<ApprovalRequest?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ApprovalRequest>.Filter.Eq(x => x.Id, id)
                     & Builders<ApprovalRequest>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<ApprovalRequest>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.ApprovalRequests.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalRequest>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<ApprovalRequest>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<ApprovalRequest>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.ApprovalRequests.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        await _context.ApprovalRequests.InsertOneAsync(request, options: null, cancellationToken);
    }

    public async Task UpdateAsync(ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        await _context.ApprovalRequests.ReplaceOneAsync(x => x.Id == request.Id, request, cancellationToken: cancellationToken);
    }
}
