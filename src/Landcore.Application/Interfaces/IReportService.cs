using Landcore.Application.DTOs.Reports;

namespace Landcore.Application.Interfaces;

public interface IReportService
{
    Task<DailyCollectionReportDto> GetDailyCollectionReportAsync(string adminId, DateTime date, CancellationToken cancellationToken = default);

    Task<MonthlyProfitReportDto> GetMonthlyProfitReportAsync(string adminId, int year, int month, CancellationToken cancellationToken = default);

    Task<AgingReportDto> GetAgingReportAsync(string adminId, CancellationToken cancellationToken = default);

    Task<PlatformSummaryReportDto> GetPlatformSummaryReportAsync(CancellationToken cancellationToken = default);
}
