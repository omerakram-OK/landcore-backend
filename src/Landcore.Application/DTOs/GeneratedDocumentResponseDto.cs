namespace Landcore.Application.DTOs;

public sealed record GeneratedDocumentResponseDto(
    string Id,
    string AdminId,
    string PlotId,
    string ClientId,
    string? BookingId,
    string DocumentType,
    DateTime GeneratedAt,
    string GeneratedBy,
    DateTime CreatedAt);
