namespace Landcore.Application.DTOs;

public sealed record BulkImportBlocksResultDto(
    int TotalRows,
    int SuccessCount,
    int FailureCount,
    List<BulkImportBlockRowResultDto> Rows);

public sealed record BulkImportBlockRowResultDto(
    int RowNumber,
    string Name,
    bool Success,
    string? Error);
