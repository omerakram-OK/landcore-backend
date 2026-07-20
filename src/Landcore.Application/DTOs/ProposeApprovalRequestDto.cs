namespace Landcore.Application.DTOs;

public sealed record ProposeApprovalRequestDto(
    string Type,
    string? TargetPlotId,
    string Justification,
    string? PayloadJson);
