namespace Landcore.Application.DTOs;

public sealed record UpdateSocietyRequestDto(
    string Name,
    string Address,
    string Description,
    int TotalPlots);
