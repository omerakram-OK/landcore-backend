using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface IReceiptRepository
{
    Task<Receipt?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default);

    Task<Receipt?> GetByPaymentIdAsync(ObjectId adminId, ObjectId paymentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Receipt>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);

    Task CreateAsync(Receipt receipt, CancellationToken cancellationToken = default);

    Task<long> CountByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);
}
