using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface IReceiptService
{
    Task<IReadOnlyList<ReceiptResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default);

    Task<ReceiptResponseDto> GetByIdAsync(string adminId, string receiptId, CancellationToken cancellationToken = default);
}
