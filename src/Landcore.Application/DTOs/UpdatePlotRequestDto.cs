namespace Landcore.Application.DTOs;

public sealed record UpdatePlotRequestDto(
    string PlotNumber,
    string BlockId,
    decimal Size,
    string SizeUnit,
    string Category,
    decimal BasePrice,
    List<string>? OwnerClientIds);
