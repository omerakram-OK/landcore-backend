using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Booking>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);

    Task<Booking?> GetActiveByPlotIdAsync(ObjectId adminId, ObjectId plotId, CancellationToken cancellationToken = default);

    Task<Booking?> GetMostRecentByPlotIdAsync(ObjectId adminId, ObjectId plotId, CancellationToken cancellationToken = default);

    Task CreateAsync(Booking booking, CancellationToken cancellationToken = default);

    Task UpdateAsync(Booking booking, CancellationToken cancellationToken = default);
}
