using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface IGeneratedDocumentRepository
{
    Task<GeneratedDocument?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GeneratedDocument>> GetByPlotIdAsync(ObjectId adminId, ObjectId plotId, CancellationToken cancellationToken = default);

    Task CreateAsync(GeneratedDocument document, CancellationToken cancellationToken = default);
}
