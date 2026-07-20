namespace Landcore.Application.DTOs;

public sealed record EmployeeResponseDto(
    string Id,
    string AdminId,
    string FullName,
    string Email,
    string Phone,
    string DesignationId,
    string DesignationName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
