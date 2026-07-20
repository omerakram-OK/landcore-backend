namespace Landcore.Application.DTOs;

public sealed record MergePlotsRequestDto(
    List<string> SourcePlotIds,
    NewPlotDefinitionDto? NewPlot,
    string? Notes,
    string? Justification);
