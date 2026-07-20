namespace Landcore.Application.DTOs.Reports;

public sealed record DailyCollectionReportDto(
    DateTime Date,
    int TotalCount,
    decimal TotalAmount,
    IReadOnlyList<PaymentModeBreakdownDto> ByMode);
