namespace Landcore.Application.DTOs;

public sealed record UpdateEmployeeRequestDto(
    string FullName,
    string Email,
    string Phone,
    string DesignationId);
