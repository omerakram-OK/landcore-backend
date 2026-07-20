namespace Landcore.Application.DTOs;

public sealed record CreateBookingRequestDto(
    string PlotId,
    string ClientId,
    string? LeadId,
    string? AgentId,
    decimal TokenAmount,
    DateTime? ExpiryDate);
