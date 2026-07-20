namespace Landcore.Application.DTOs;

public sealed record CreateAgentRequestDto(
    string FullName,
    string CNIC,
    string Phone,
    string Email,
    string Address,
    string CommissionType,
    decimal CommissionValue);
