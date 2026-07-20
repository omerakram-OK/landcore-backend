namespace Landcore.Application.DTOs;

public sealed record PermissionDto(string Module, List<string> Actions);
