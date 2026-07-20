namespace Landcore.Application.DTOs;

public sealed record UpdateAgentRequestDto(
    string FullName,
    string CNIC,
    string Phone,
    string Email,
    string Address,
    string CommissionType,
    decimal CommissionValue);
