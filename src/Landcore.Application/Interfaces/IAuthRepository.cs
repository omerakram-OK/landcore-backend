using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface IAuthRepository
{
    Task<SuperMan?> FindSuperManByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<Admin?> FindAdminByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<Employee?> FindEmployeeByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<Subscription?> GetSubscriptionByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);

    Task<Designation?> GetDesignationByIdAsync(ObjectId designationId, CancellationToken cancellationToken = default);
}
