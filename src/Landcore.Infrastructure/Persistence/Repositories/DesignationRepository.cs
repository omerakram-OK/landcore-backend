using System.Text.RegularExpressions;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class DesignationRepository : IDesignationRepository
{
    private readonly MongoDbContext _context;

    public DesignationRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Designation?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Designation>.Filter.Eq(x => x.Id, id)
                     & Builders<Designation>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Designation>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Designations.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Designation?> GetByNameAsync(ObjectId adminId, string name, CancellationToken cancellationToken = default)
    {
        var escaped = Regex.Escape(name.Trim());
        var filter = Builders<Designation>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Designation>.Filter.Regex(x => x.Name, new BsonRegularExpression($"^{escaped}$", "i"))
                     & Builders<Designation>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Designations.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Designation>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Designation>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Designation>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Designations.Find(filter).SortBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Designation designation, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Designations.InsertOneAsync(designation, options: null, cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw DuplicateNameException();
        }
    }

    public async Task UpdateAsync(Designation designation, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Designations.ReplaceOneAsync(x => x.Id == designation.Id, designation, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw DuplicateNameException();
        }
    }

    public async Task<bool> SoftDeleteAsync(ObjectId adminId, ObjectId id, ObjectId deletedBy, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Designation>.Filter.Eq(x => x.Id, id)
                     & Builders<Designation>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Designation>.Filter.Eq(x => x.IsDeleted, false);

        var update = Builders<Designation>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.DeletedAt, DateTime.UtcNow)
            .Set(x => x.DeletedBy, deletedBy)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedBy, deletedBy);

        var result = await _context.Designations.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    private static ValidationAppException DuplicateNameException() => new(
        "A Designation with this name already exists.",
        new Dictionary<string, string[]> { ["Name"] = ["A Designation with this name already exists."] });
}
