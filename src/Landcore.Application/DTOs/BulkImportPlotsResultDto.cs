namespace Landcore.Application.DTOs;

public sealed record BulkImportPlotsResultDto(
    int TotalRows,
    int SuccessCount,
    int FailureCount,
    List<BulkImportPlotRowResultDto> Rows);

public sealed record BulkImportPlotRowResultDto(
    int RowNumber,
    string PlotNumber,
    bool Success,
    string? Error);
