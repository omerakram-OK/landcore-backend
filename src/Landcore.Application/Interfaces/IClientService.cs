using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface IClientService
{
    Task<ClientResponseDto> CreateAsync(string adminId, CreateClientRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default);

    Task<ClientResponseDto> GetByIdAsync(string adminId, string clientId, CancellationToken cancellationToken = default);

    Task<ClientResponseDto> UpdateAsync(string adminId, string clientId, UpdateClientRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task DeleteAsync(string adminId, string clientId, string performedByUserId, CancellationToken cancellationToken = default);
}
