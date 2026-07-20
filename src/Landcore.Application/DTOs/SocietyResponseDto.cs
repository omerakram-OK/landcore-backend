namespace Landcore.Application.DTOs;

public sealed record SocietyResponseDto(
    string Id,
    string AdminId,
    string Name,
    string Address,
    string Description,
    int TotalPlots,
    DateTime CreatedAt,
    DateTime UpdatedAt);
