using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface IPlotRepository
{
    Task<Plot?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default);

    Task<Plot?> GetByPlotNumberAsync(ObjectId adminId, ObjectId blockId, string plotNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Plot>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Plot>> GetAllBySocietyIdAsync(ObjectId adminId, ObjectId societyId, CancellationToken cancellationToken = default);

    Task CreateAsync(Plot plot, CancellationToken cancellationToken = default);

    Task UpdateAsync(Plot plot, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(ObjectId adminId, ObjectId id, ObjectId deletedBy, CancellationToken cancellationToken = default);

    Task<long> CountAllAsync(CancellationToken cancellationToken = default);

    Task<long> CountAllByStatusAsync(PlotStatus status, CancellationToken cancellationToken = default);
}
