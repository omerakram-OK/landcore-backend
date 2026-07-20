namespace Landcore.Application.DTOs;

public sealed record SplitPlotRequestDto(
    List<NewPlotDefinitionDto> NewPlots,
    string? Notes,
    string? Justification);
