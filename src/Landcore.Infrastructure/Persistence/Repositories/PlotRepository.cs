using System.Text.RegularExpressions;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class PlotRepository : IPlotRepository
{
    private readonly MongoDbContext _context;

    public PlotRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Plot?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Plot>.Filter.Eq(x => x.Id, id)
                     & Builders<Plot>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Plot>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Plots.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Plot?> GetByPlotNumberAsync(ObjectId adminId, ObjectId blockId, string plotNumber, CancellationToken cancellationToken = default)
    {
        var escaped = Regex.Escape(plotNumber.Trim());
        var filter = Builders<Plot>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Plot>.Filter.Eq(x => x.BlockId, blockId)
                     & Builders<Plot>.Filter.Regex(x => x.PlotNumber, new BsonRegularExpression($"^{escaped}$", "i"))
                     & Builders<Plot>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Plots.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Plot>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Plot>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Plot>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Plots.Find(filter).SortBy(x => x.PlotNumber).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Plot>> GetAllBySocietyIdAsync(ObjectId adminId, ObjectId societyId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Plot>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Plot>.Filter.Eq(x => x.SocietyId, societyId)
                     & Builders<Plot>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Plots.Find(filter).SortBy(x => x.PlotNumber).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Plot plot, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Plots.InsertOneAsync(plot, options: null, cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw DuplicatePlotNumberException();
        }
    }

    public async Task UpdateAsync(Plot plot, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Plots.ReplaceOneAsync(x => x.Id == plot.Id, plot, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw DuplicatePlotNumberException();
        }
    }

    public async Task<bool> SoftDeleteAsync(ObjectId adminId, ObjectId id, ObjectId deletedBy, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Plot>.Filter.Eq(x => x.Id, id)
                     & Builders<Plot>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Plot>.Filter.Eq(x => x.IsDeleted, false);

        var update = Builders<Plot>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.DeletedAt, DateTime.UtcNow)
            .Set(x => x.DeletedBy, deletedBy)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedBy, deletedBy);

        var result = await _context.Plots.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task<long> CountAllAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Plot>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Plots.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<long> CountAllByStatusAsync(PlotStatus status, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Plot>.Filter.Eq(x => x.Status, status)
                     & Builders<Plot>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Plots.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    private static ValidationAppException DuplicatePlotNumberException() => new(
        "A Plot with this number already exists in this Block.",
        new Dictionary<string, string[]> { ["PlotNumber"] = ["A Plot with this number already exists in this Block."] });
}
