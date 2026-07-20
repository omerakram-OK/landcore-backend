using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface IRepossessionService
{
    Task<RepossessionScanResultDto> ScanAndFlagOverdueAsync(string adminId, string performedByUserId, CancellationToken cancellationToken = default);

    Task<PlotResponseDto> ResumePlanAsync(string adminId, string plotId, string? notes, string performedByUserId, CancellationToken cancellationToken = default);

    Task<RefundRecordResponseDto> RecordLatePaymentAsync(string adminId, string plotId, RecordLatePaymentRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<RefundRecordResponseDto> IssueRefundAsync(string adminId, string refundRecordId, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefundRecordResponseDto>> GetAllRefundsAsync(string adminId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RefundRecordResponseDto>> GetRefundsByPlotIdAsync(string adminId, string plotId, CancellationToken cancellationToken = default);

    Task<RefundRecordResponseDto> GetRefundByIdAsync(string adminId, string refundRecordId, CancellationToken cancellationToken = default);
}
