using System.Text.RegularExpressions;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly MongoDbContext _context;

    public EmployeeRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Employee?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Employee>.Filter.Eq(x => x.Id, id)
                     & Builders<Employee>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Employee>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Employees.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Employee?> GetByEmailAsync(ObjectId adminId, string email, CancellationToken cancellationToken = default)
    {
        var escaped = Regex.Escape(email.Trim());
        var filter = Builders<Employee>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Employee>.Filter.Regex(x => x.Email, new BsonRegularExpression($"^{escaped}$", "i"))
                     & Builders<Employee>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Employees.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Employee>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Employee>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Employees.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Employees.InsertOneAsync(employee, options: null, cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw DuplicateEmailException();
        }
    }

    public async Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Employees.ReplaceOneAsync(x => x.Id == employee.Id, employee, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw DuplicateEmailException();
        }
    }

    public async Task<bool> SoftDeleteAsync(ObjectId adminId, ObjectId id, ObjectId deletedBy, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Employee>.Filter.Eq(x => x.Id, id)
                     & Builders<Employee>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Employee>.Filter.Eq(x => x.IsDeleted, false);

        var update = Builders<Employee>.Update
            .Set(x => x.IsDeleted, true)
            .Set(x => x.DeletedAt, DateTime.UtcNow)
            .Set(x => x.DeletedBy, deletedBy)
            .Set(x => x.UpdatedAt, DateTime.UtcNow)
            .Set(x => x.UpdatedBy, deletedBy);

        var result = await _context.Employees.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task<long> CountActiveByDesignationIdAsync(ObjectId adminId, ObjectId designationId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Employee>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Employee>.Filter.Eq(x => x.DesignationId, designationId)
                     & Builders<Employee>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Employees.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    private static ValidationAppException DuplicateEmailException() => new(
        "An Employee with this email already exists.",
        new Dictionary<string, string[]> { ["Email"] = ["An Employee with this email already exists."] });
}
