namespace Landcore.Application.DTOs;

public sealed record CreateDesignationRequestDto(string Name, List<PermissionDto> Permissions);
