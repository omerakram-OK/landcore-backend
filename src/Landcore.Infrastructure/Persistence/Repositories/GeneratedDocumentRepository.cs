using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class GeneratedDocumentRepository : IGeneratedDocumentRepository
{
    private readonly MongoDbContext _context;

    public GeneratedDocumentRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<GeneratedDocument?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GeneratedDocument>.Filter.Eq(x => x.Id, id)
                     & Builders<GeneratedDocument>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<GeneratedDocument>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.GeneratedDocuments.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GeneratedDocument>> GetByPlotIdAsync(ObjectId adminId, ObjectId plotId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GeneratedDocument>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<GeneratedDocument>.Filter.Eq(x => x.PlotId, plotId)
                     & Builders<GeneratedDocument>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.GeneratedDocuments.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(GeneratedDocument document, CancellationToken cancellationToken = default)
    {
        await _context.GeneratedDocuments.InsertOneAsync(document, options: null, cancellationToken);
    }
}
