namespace Landcore.Application.DTOs;

public sealed record DesignationResponseDto(
    string Id,
    string AdminId,
    string Name,
    List<PermissionDto> Permissions,
    DateTime CreatedAt,
    DateTime UpdatedAt);
