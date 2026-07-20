namespace Landcore.Application.DTOs.Reports;

public sealed record MonthlyProfitReportDto(
    int Year,
    int Month,
    int PaymentCount,
    decimal TotalCollected,
    int RefundCount,
    decimal TotalCompanyProfitShareFromRefunds,
    decimal NetProfit);
