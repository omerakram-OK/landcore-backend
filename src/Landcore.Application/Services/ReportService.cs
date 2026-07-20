using Landcore.Application.DTOs;
using Landcore.Application.DTOs.Reports;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class ReportService : IReportService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IRefundRecordRepository _refundRecordRepository;
    private readonly IInstallmentPlanRepository _installmentPlanRepository;
    private readonly IPlotRepository _plotRepository;
    private readonly IAdminRepository _adminRepository;

    public ReportService(
        IPaymentRepository paymentRepository,
        IRefundRecordRepository refundRecordRepository,
        IInstallmentPlanRepository installmentPlanRepository,
        IPlotRepository plotRepository,
        IAdminRepository adminRepository)
    {
        _paymentRepository = paymentRepository;
        _refundRecordRepository = refundRecordRepository;
        _installmentPlanRepository = installmentPlanRepository;
        _plotRepository = plotRepository;
        _adminRepository = adminRepository;
    }

    public async Task<DailyCollectionReportDto> GetDailyCollectionReportAsync(string adminId, DateTime date, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var day = date.Date;

        var allPayments = await _paymentRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        var matching = allPayments.Where(payment => payment.Date.Date == day).ToList();

        var byMode = matching
            .GroupBy(payment => payment.Mode)
            .Select(paymentGroup => new PaymentModeBreakdownDto(paymentGroup.Key.ToString(), paymentGroup.Count(), paymentGroup.Sum(payment => (decimal)payment.Amount)))
            .OrderBy(dto => dto.Mode)
            .ToList();

        return new DailyCollectionReportDto(day, matching.Count, matching.Sum(payment => (decimal)payment.Amount), byMode);
    }

    public async Task<MonthlyProfitReportDto> GetMonthlyProfitReportAsync(string adminId, int year, int month, CancellationToken cancellationToken = default)
    {
        if (month is < 1 or > 12)
        {
            throw new ValidationAppException(
                "'month' must be between 1 and 12.",
                new Dictionary<string, string[]> { ["month"] = ["'month' must be between 1 and 12."] });
        }

        var adminObjectId = ParseObjectId(adminId, "adminId");

        var allPayments = await _paymentRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        var matchingPayments = allPayments.Where(payment => payment.Date.Year == year && payment.Date.Month == month).ToList();
        var totalCollected = matchingPayments.Sum(payment => (decimal)payment.Amount);

        var allRefunds = await _refundRecordRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        var matchingRefunds = allRefunds
            .Where(refund => refund.Status == RefundRecordStatus.Issued
                        && refund.IssuedAt.HasValue
                        && refund.IssuedAt.Value.Year == year
                        && refund.IssuedAt.Value.Month == month)
            .ToList();
        var totalCompanyProfitShare = matchingRefunds.Sum(refund => (decimal)refund.CompanyProfitAmount);

        return new MonthlyProfitReportDto(
            year,
            month,
            matchingPayments.Count,
            totalCollected,
            matchingRefunds.Count,
            totalCompanyProfitShare,
            totalCollected - totalCompanyProfitShare);
    }

    public async Task<AgingReportDto> GetAgingReportAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var today = DateTime.UtcNow.Date;

        var plans = await _installmentPlanRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);

        const string bucketCurrent = "Current";
        const string bucket1To30 = "1-30";
        const string bucket31To60 = "31-60";
        const string bucket61To90 = "61-90";
        const string bucket90Plus = "90+";

        var order = new[] { bucketCurrent, bucket1To30, bucket31To60, bucket61To90, bucket90Plus };
        var counts = order.ToDictionary(bucket => bucket, _ => 0);
        var amounts = order.ToDictionary(bucket => bucket, _ => 0m);

        foreach (var plan in plans)
        {
            foreach (var installment in plan.Installments)
            {
                var outstanding = (decimal)installment.Amount - (decimal)installment.PaidAmount;
                if (outstanding <= 0m)
                {
                    continue;
                }

                var bucket = ClassifyAgingBucket(installment.DueDate.Date, today, bucketCurrent, bucket1To30, bucket31To60, bucket61To90, bucket90Plus);
                counts[bucket]++;
                amounts[bucket] += outstanding;
            }
        }

        var buckets = order.Select(bucket => new AgingBucketDto(bucket, counts[bucket], amounts[bucket])).ToList();

        return new AgingReportDto(buckets, counts.Values.Sum(), amounts.Values.Sum());
    }

    private static string ClassifyAgingBucket(DateTime dueDate, DateTime today, string current, string b1To30, string b31To60, string b61To90, string b90Plus)
    {
        if (dueDate >= today)
        {
            return current;
        }

        var overdueDays = (today - dueDate).Days;
        return overdueDays switch
        {
            <= 30 => b1To30,
            <= 60 => b31To60,
            <= 90 => b61To90,
            _ => b90Plus,
        };
    }

    public async Task<PlatformSummaryReportDto> GetPlatformSummaryReportAsync(CancellationToken cancellationToken = default)
    {
        var admins = await _adminRepository.GetAllAsync(cancellationToken);
        var totalActiveAdmins = admins.Count(admin => admin.Status == AdminStatus.Active);

        var totalPlots = await _plotRepository.CountAllAsync(cancellationToken);
        var totalOverduePlots = await _plotRepository.CountAllByStatusAsync(PlotStatus.Overdue, cancellationToken);

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
        var totalPaymentsThisMonth = await _paymentRepository.SumAmountAcrossAllAdminsAsync(monthStart, monthEnd, cancellationToken);

        return new PlatformSummaryReportDto(
            admins.Count,
            totalActiveAdmins,
            totalPlots,
            totalOverduePlots,
            now.Year,
            now.Month,
            totalPaymentsThisMonth);
    }

    private static ObjectId ParseObjectId(string value, string fieldName)
    {
        if (!ObjectId.TryParse(value, out var id))
        {
            throw new ValidationAppException(
                $"'{fieldName}' is not a valid identifier.",
                new Dictionary<string, string[]> { [fieldName] = [$"'{value}' is not a valid identifier."] });
        }

        return id;
    }
}
