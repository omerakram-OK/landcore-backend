using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface IDesignationService
{
    Task<DesignationResponseDto> CreateAsync(string adminId, CreateDesignationRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DesignationResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default);

    Task<DesignationResponseDto> GetByIdAsync(string adminId, string designationId, CancellationToken cancellationToken = default);

    Task<DesignationResponseDto> UpdateAsync(string adminId, string designationId, UpdateDesignationRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task DeleteAsync(string adminId, string designationId, string performedByUserId, CancellationToken cancellationToken = default);
}
