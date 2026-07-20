namespace Landcore.Application.DTOs;

public sealed record RefundRecordResponseDto(
    string Id,
    string AdminId,
    string PlotId,
    string BookingId,
    string InstallmentPlanId,
    string ClientId,
    decimal AmountPaid,
    decimal CompanyProfitAmount,
    decimal ClientRefundAmount,
    DateTime PaymentDate,
    string Status,
    DateTime? IssuedAt,
    string? IssuedBy,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);
