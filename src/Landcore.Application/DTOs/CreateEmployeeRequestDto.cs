namespace Landcore.Application.DTOs;

public sealed record CreateEmployeeRequestDto(
    string FullName,
    string Email,
    string Phone,
    string InitialPassword,
    string DesignationId);
