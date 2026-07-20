using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponseDto> RecordPaymentAsync(string adminId, RecordPaymentRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default);

    Task<PaymentResponseDto> GetByIdAsync(string adminId, string paymentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentResponseDto>> GetByInstallmentPlanIdAsync(string adminId, string installmentPlanId, CancellationToken cancellationToken = default);
}
