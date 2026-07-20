namespace Landcore.Application.DTOs;

public sealed record PlotHistoryLogEntryDto(
    string Event,
    string Details,
    DateTime At,
    string By);
