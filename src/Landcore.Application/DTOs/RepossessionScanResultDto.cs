namespace Landcore.Application.DTOs;

public sealed record RepossessionScanResultDto(
    List<string> NewlyOverduePlotIds,
    List<string> AutoRepossessedPlotIds);
