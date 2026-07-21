using System.Text.RegularExpressions;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly MongoDbContext _context;

    public AdminRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Admin?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Admin>.Filter.Eq(x => x.Id, id)
                     & Builders<Admin>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Admins.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Admin?> GetByContactEmailAsync(string contactEmail, CancellationToken cancellationToken = default)
    {
        var escaped = Regex.Escape(contactEmail.Trim());
        var filter = Builders<Admin>.Filter.Regex(x => x.ContactEmail, new BsonRegularExpression($"^{escaped}$", "i"))
                     & Builders<Admin>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Admins.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Admin>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Admin>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Admins.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Admin admin, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Admins.InsertOneAsync(admin, options: null, cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new ValidationAppException(
                "An Admin with this contact email already exists.",
                new Dictionary<string, string[]> { ["ContactEmail"] = ["An Admin with this contact email already exists."] });
        }
    }

    public async Task UpdateAsync(Admin admin, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Admins.ReplaceOneAsync(x => x.Id == admin.Id, admin, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new ValidationAppException(
                "An Admin with this contact email already exists.",
                new Dictionary<string, string[]> { ["ContactEmail"] = ["An Admin with this contact email already exists."] });
        }
    }

    public async Task UpdateStatusAsync(ObjectId id, AdminStatus status, ObjectId updatedBy, CancellationToken cancellationToken = default)
    {
        var update = Builders<Admin>.Update
            .Set(x => x.Status, status)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedBy, updatedBy);

        await _context.Admins.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
    }

    public async Task SetSubscriptionIdAsync(ObjectId adminId, ObjectId subscriptionId, ObjectId updatedBy, CancellationToken cancellationToken = default)
    {
        var update = Builders<Admin>.Update
            .Set(x => x.SubscriptionId, subscriptionId)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedBy, updatedBy);

        await _context.Admins.UpdateOneAsync(x => x.Id == adminId, update, cancellationToken: cancellationToken);
    }

    public async Task SetLogoAsync(ObjectId adminId, string? logoBase64, string? logoContentType, ObjectId updatedBy, CancellationToken cancellationToken = default)
    {
        var update = Builders<Admin>.Update
            .Set(x => x.LogoBase64, logoBase64)
            .Set(x => x.LogoContentType, logoContentType)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedBy, updatedBy);

        await _context.Admins.UpdateOneAsync(x => x.Id == adminId, update, cancellationToken: cancellationToken);
    }
}
