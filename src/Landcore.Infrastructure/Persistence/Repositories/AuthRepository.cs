using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using Landcore.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Landcore.Infrastructure.Persistence.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly MongoDbContext _context;

    public AuthRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<SuperMan?> FindSuperManByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var filter = ActiveEmailFilter<SuperMan>(x => x.Email, email);
        return await _context.SuperMen.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Admin?> FindAdminByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var filter = ActiveEmailFilter<Admin>(x => x.ContactEmail, email);
        return await _context.Admins.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Employee?> FindEmployeeByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var filter = ActiveEmailFilter<Employee>(x => x.Email, email);
        return await _context.Employees.Find(filter)
            .SortBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Subscription?> GetSubscriptionByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Subscription>.Filter.Eq(x => x.AdminId, adminId)
                     & Builders<Subscription>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Subscriptions.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Designation?> GetDesignationByIdAsync(ObjectId designationId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Designation>.Filter.Eq(x => x.Id, designationId)
                     & Builders<Designation>.Filter.Eq(x => x.IsDeleted, false);
        return await _context.Designations.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    private static FilterDefinition<T> ActiveEmailFilter<T>(
        Expression<Func<T, string>> emailField, string email)
    {
        var escaped = Regex.Escape(email.Trim());
        var fieldDefinition = new ExpressionFieldDefinition<T, string>(emailField);
        var emailFilter = Builders<T>.Filter.Regex(fieldDefinition, new BsonRegularExpression($"^{escaped}$", "i"));
        var notDeletedFilter = Builders<T>.Filter.Eq("IsDeleted", false);
        return emailFilter & notDeletedFilter;
    }
}
