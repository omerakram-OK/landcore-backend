namespace Landcore.Application.DTOs;

public sealed record CreateBlockRequestDto(
    string SocietyId,
    string Name,
    string Description,
    int TotalPlots);
