namespace Landcore.Application.DTOs;

public sealed record BookingResponseDto(
    string Id,
    string AdminId,
    string PlotId,
    string ClientId,
    string? LeadId,
    string? AgentId,
    string? CommissionType,
    decimal? CommissionValue,
    decimal TokenAmount,
    DateTime ExpiryDate,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
