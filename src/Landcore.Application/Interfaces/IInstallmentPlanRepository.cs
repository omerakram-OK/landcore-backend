using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface IInstallmentPlanRepository
{
    Task<InstallmentPlan?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default);

    Task<InstallmentPlan?> GetByBookingIdAsync(ObjectId adminId, ObjectId bookingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InstallmentPlan>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);

    Task CreateAsync(InstallmentPlan plan, CancellationToken cancellationToken = default);

    Task UpdateAsync(InstallmentPlan plan, CancellationToken cancellationToken = default);
}
