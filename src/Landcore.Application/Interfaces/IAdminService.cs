using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface IAdminService
{
    Task<AdminResponseDto> CreateAsync(CreateAdminRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AdminResponseDto> GetByIdAsync(string adminId, CancellationToken cancellationToken = default);

    Task<AdminResponseDto> UpdateAsync(string adminId, UpdateAdminRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<AdminResponseDto> SuspendAsync(string adminId, string performedByUserId, CancellationToken cancellationToken = default);

    Task<AdminResponseDto> ReactivateAsync(string adminId, string performedByUserId, CancellationToken cancellationToken = default);
}
