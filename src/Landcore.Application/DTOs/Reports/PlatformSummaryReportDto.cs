namespace Landcore.Application.DTOs.Reports;

public sealed record PlatformSummaryReportDto(
    int TotalAdmins,
    int TotalActiveAdmins,
    long TotalPlots,
    long TotalOverduePlots,
    int Year,
    int Month,
    decimal TotalPaymentsCollectedThisMonth);
