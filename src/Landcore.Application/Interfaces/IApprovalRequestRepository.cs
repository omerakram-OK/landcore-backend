using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface IApprovalRequestRepository
{
    Task<ApprovalRequest?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApprovalRequest>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);

    Task CreateAsync(ApprovalRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(ApprovalRequest request, CancellationToken cancellationToken = default);
}
