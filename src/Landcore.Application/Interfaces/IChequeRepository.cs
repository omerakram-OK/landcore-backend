using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface IChequeRepository
{
    Task<Cheque?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default);

    Task<Cheque?> GetByPaymentIdAsync(ObjectId adminId, ObjectId paymentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Cheque>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);

    Task CreateAsync(Cheque cheque, CancellationToken cancellationToken = default);

    Task UpdateAsync(Cheque cheque, CancellationToken cancellationToken = default);
}
