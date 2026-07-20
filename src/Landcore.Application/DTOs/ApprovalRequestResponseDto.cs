namespace Landcore.Application.DTOs;

public sealed record ApprovalRequestResponseDto(
    string Id,
    string AdminId,
    string Type,
    string RequestedByEmployeeId,
    string? TargetPlotId,
    string Justification,
    string Status,
    string? DecidedByAdminId,
    string? DecisionNotes,
    string? PayloadJson,
    DateTime CreatedAt,
    DateTime UpdatedAt);
