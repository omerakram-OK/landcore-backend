namespace Landcore.Application.DTOs;

public sealed record UpdatePlotPossessionStatusRequestDto(
    string PossessionStatus,
    string? Notes);
