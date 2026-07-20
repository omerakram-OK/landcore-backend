namespace Landcore.Application.DTOs;

public sealed record ChangePlotStatusRequestDto(
    string Status,
    string? Notes);
