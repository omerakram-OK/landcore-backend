namespace Landcore.Application.DTOs;

public sealed record CreateSocietyRequestDto(
    string Name,
    string Address,
    string Description,
    int TotalPlots);
