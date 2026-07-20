namespace Landcore.Application.DTOs;

public sealed record CreateSubscriptionRequestDto(
    string AdminId,
    string Plan,
    decimal FeeAmount,
    DateTime StartDate,
    DateTime NextDueDate,
    string? Status = null);
