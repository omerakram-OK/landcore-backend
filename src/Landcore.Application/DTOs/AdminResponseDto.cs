namespace Landcore.Application.DTOs;

public sealed record AdminResponseDto(
    string Id,
    string SocietyName,
    string ContactEmail,
    string Status,
    string? SubscriptionId,
    DateTime CreatedAt,
    DateTime UpdatedAt);
