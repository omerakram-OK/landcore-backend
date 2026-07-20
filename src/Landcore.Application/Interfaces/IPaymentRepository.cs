using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Payment>> GetByInstallmentPlanIdAsync(ObjectId adminId, ObjectId installmentPlanId, CancellationToken cancellationToken = default);

    Task CreateAsync(Payment payment, CancellationToken cancellationToken = default);

    Task<decimal> SumAmountAcrossAllAdminsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
