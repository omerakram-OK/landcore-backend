namespace Landcore.Application.DTOs;

public sealed record ClientResponseDto(
    string Id,
    string AdminId,
    string FullName,
    string CNIC,
    List<string> Phones,
    string Email,
    string Address,
    string EmergencyContact,
    string? LinkedAgentId,
    List<string> CoOwnerClientIds,
    DateTime CreatedAt,
    DateTime UpdatedAt);
