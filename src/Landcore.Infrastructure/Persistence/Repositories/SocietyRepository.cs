using System.Text.RegularExpressions;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class SocietyRepository : ISocietyRepository
{
    private readonly MongoDbContext _context;

    public SocietyRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Society?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Society>.Filter.Eq(x => x.Id, id)
                     & Builders<Society>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Society>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Societies.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Society?> GetByNameAsync(ObjectId adminId, string name, CancellationToken cancellationToken = default)
    {
        var escaped = Regex.Escape(name.Trim());
        var filter = Builders<Society>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Society>.Filter.Regex(x => x.Name, new BsonRegularExpression($"^{escaped}$", "i"))
                     & Builders<Society>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Societies.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Society>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Society>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Society>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Societies.Find(filter).SortBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Society society, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Societies.InsertOneAsync(society, options: null, cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw DuplicateNameException();
        }
    }

    public async Task UpdateAsync(Society society, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Societies.ReplaceOneAsync(x => x.Id == society.Id, society, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw DuplicateNameException();
        }
    }

    public async Task<bool> SoftDeleteAsync(ObjectId adminId, ObjectId id, ObjectId deletedBy, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Society>.Filter.Eq(x => x.Id, id)
                     & Builders<Society>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Society>.Filter.Eq(x => x.IsDeleted, false);

        var update = Builders<Society>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.DeletedAt, DateTime.UtcNow)
            .Set(x => x.DeletedBy, deletedBy)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedBy, deletedBy);

        var result = await _context.Societies.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    private static ValidationAppException DuplicateNameException() => new(
        "A Society with this name already exists.",
        new Dictionary<string, string[]> { ["Name"] = ["A Society with this name already exists."] });
}
