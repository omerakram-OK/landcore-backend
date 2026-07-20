namespace Landcore.Application.DTOs;

public sealed record UpdateLeadRequestDto(
    string Name,
    string Phone,
    string Email,
    string Source,
    string? InterestedPlotId,
    string AssignedEmployeeId);
