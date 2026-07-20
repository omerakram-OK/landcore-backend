namespace Landcore.Application.DTOs;

public sealed record LeadResponseDto(
    string Id,
    string AdminId,
    string Name,
    string Phone,
    string Email,
    string Source,
    string? InterestedPlotId,
    string Status,
    string AssignedEmployeeId,
    string AssignedEmployeeName,
    List<FollowUpNoteDto> FollowUpNotes,
    DateTime CreatedAt,
    DateTime UpdatedAt);
