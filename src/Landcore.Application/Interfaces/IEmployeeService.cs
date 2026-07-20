using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface IEmployeeService
{
    Task<EmployeeResponseDto> CreateAsync(string adminId, CreateEmployeeRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default);

    Task<EmployeeResponseDto> GetByIdAsync(string adminId, string employeeId, CancellationToken cancellationToken = default);

    Task<EmployeeResponseDto> UpdateAsync(string adminId, string employeeId, UpdateEmployeeRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<EmployeeResponseDto> DeactivateAsync(string adminId, string employeeId, string performedByUserId, CancellationToken cancellationToken = default);
}
