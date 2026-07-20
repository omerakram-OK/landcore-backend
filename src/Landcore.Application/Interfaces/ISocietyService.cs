using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface ISocietyService
{
    Task<SocietyResponseDto> CreateAsync(string adminId, CreateSocietyRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SocietyResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default);

    Task<SocietyResponseDto> GetByIdAsync(string adminId, string societyId, CancellationToken cancellationToken = default);

    Task<SocietyResponseDto> UpdateAsync(string adminId, string societyId, UpdateSocietyRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task DeleteAsync(string adminId, string societyId, string performedByUserId, CancellationToken cancellationToken = default);
}
