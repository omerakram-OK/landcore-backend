namespace Landcore.Application.DTOs.Reports;

public sealed record AgingReportDto(
    IReadOnlyList<AgingBucketDto> Buckets,
    int TotalOutstandingCount,
    decimal TotalOutstandingAmount);

public sealed record AgingBucketDto(
    string Bucket,
    int Count,
    decimal OutstandingAmount);
