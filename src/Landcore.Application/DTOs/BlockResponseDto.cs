namespace Landcore.Application.DTOs;

public sealed record BlockResponseDto(
    string Id,
    string AdminId,
    string SocietyId,
    string Name,
    string Description,
    int TotalPlots,
    DateTime CreatedAt,
    DateTime UpdatedAt);
