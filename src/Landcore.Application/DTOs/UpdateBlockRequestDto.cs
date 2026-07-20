namespace Landcore.Application.DTOs;

public sealed record UpdateBlockRequestDto(
    string SocietyId,
    string Name,
    string Description,
    int TotalPlots);
