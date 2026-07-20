namespace Landcore.Application.DTOs;

public sealed record SubscriptionResponseDto(
    string Id,
    string AdminId,
    string Plan,
    decimal FeeAmount,
    DateTime StartDate,
    DateTime NextDueDate,
    string Status);
