namespace Landcore.Application.DTOs;

public sealed record UpdateDesignationRequestDto(string Name, List<PermissionDto> Permissions);
