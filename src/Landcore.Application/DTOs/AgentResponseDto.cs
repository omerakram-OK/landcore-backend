namespace Landcore.Application.DTOs;

public sealed record AgentResponseDto(
    string Id,
    string AdminId,
    string FullName,
    string CNIC,
    string Phone,
    string Email,
    string Address,
    string CommissionType,
    decimal CommissionValue,
    DateTime CreatedAt,
    DateTime UpdatedAt);
