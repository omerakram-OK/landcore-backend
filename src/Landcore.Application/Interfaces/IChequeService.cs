using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface IChequeService
{
    Task<IReadOnlyList<ChequeResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default);

    Task<ChequeResponseDto> GetByIdAsync(string adminId, string chequeId, CancellationToken cancellationToken = default);

    Task<ChequeResponseDto> ClearAsync(string adminId, string chequeId, string performedByUserId, CancellationToken cancellationToken = default);

    Task<ChequeResponseDto> MarkBouncedAsync(string adminId, string chequeId, BounceChequeRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);
}
