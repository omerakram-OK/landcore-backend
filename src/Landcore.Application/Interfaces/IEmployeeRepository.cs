using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(ObjectId adminId, ObjectId id, CancellationToken cancellationToken = default);

    Task<Employee?> GetByEmailAsync(ObjectId adminId, string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Employee>> GetAllByAdminIdAsync(ObjectId adminId, CancellationToken cancellationToken = default);

    Task CreateAsync(Employee employee, CancellationToken cancellationToken = default);

    Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(ObjectId adminId, ObjectId id, ObjectId deletedBy, CancellationToken cancellationToken = default);

    Task<long> CountActiveByDesignationIdAsync(ObjectId adminId, ObjectId designationId, CancellationToken cancellationToken = default);
}
