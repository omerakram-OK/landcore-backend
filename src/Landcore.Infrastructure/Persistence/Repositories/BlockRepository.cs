using System.Text.RegularExpressions;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class BlockRepository : IBlockRepository
{
    private readonly MongoDbContext _context;

    public BlockRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Block?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Block>.Filter.Eq(x => x.Id, id)
                     & Builders<Block>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Block>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Blocks.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Block?> GetByNameAsync(ObjectId adminId, ObjectId societyId, string name, CancellationToken cancellationToken = default)
    {
        var escaped = Regex.Escape(name.Trim());
        var filter = Builders<Block>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Block>.Filter.Eq(x => x.SocietyId, societyId)
                     & Builders<Block>.Filter.Regex(x => x.Name, new BsonRegularExpression($"^{escaped}$", "i"))
                     & Builders<Block>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Blocks.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Block>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Block>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Block>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Blocks.Find(filter).SortBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Block>> GetAllBySocietyIdAsync(ObjectId adminId, ObjectId societyId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Block>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Block>.Filter.Eq(x => x.SocietyId, societyId)
                     & Builders<Block>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Blocks.Find(filter).SortBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Block block, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Blocks.InsertOneAsync(block, options: null, cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw DuplicateNameException();
        }
    }

    public async Task UpdateAsync(Block block, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Blocks.ReplaceOneAsync(x => x.Id == block.Id, block, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw DuplicateNameException();
        }
    }

    public async Task<bool> SoftDeleteAsync(ObjectId adminId, ObjectId id, ObjectId deletedBy, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Block>.Filter.Eq(x => x.Id, id)
                     & Builders<Block>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Block>.Filter.Eq(x => x.IsDeleted, false);

        var update = Builders<Block>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.DeletedAt, DateTime.UtcNow)
            .Set(x => x.DeletedBy, deletedBy)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedBy, deletedBy);

        var result = await _context.Blocks.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    private static ValidationAppException DuplicateNameException() => new(
        "A Block with this name/number already exists in this Society.",
        new Dictionary<string, string[]> { ["Name"] = ["A Block with this name/number already exists in this Society."] });
}
