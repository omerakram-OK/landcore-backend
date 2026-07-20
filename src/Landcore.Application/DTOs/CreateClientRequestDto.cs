namespace Landcore.Application.DTOs;

public sealed record CreateClientRequestDto(
    string FullName,
    string CNIC,
    List<string> Phones,
    string Email,
    string Address,
    string? EmergencyContact,
    string? LinkedAgentId,
    List<string>? CoOwnerClientIds);
