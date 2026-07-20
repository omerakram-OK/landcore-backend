using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface IRefundRecordRepository
{
    Task<RefundRecord?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefundRecord>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefundRecord>> GetByPlotIdAsync(ObjectId adminId, ObjectId plotId, CancellationToken cancellationToken = default);

    Task CreateAsync(RefundRecord record, CancellationToken cancellationToken = default);

    Task UpdateAsync(RefundRecord record, CancellationToken cancellationToken = default);
}
