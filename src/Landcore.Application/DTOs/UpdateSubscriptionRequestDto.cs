namespace Landcore.Application.DTOs;

public sealed record UpdateSubscriptionRequestDto(
    string Plan,
    decimal FeeAmount,
    DateTime StartDate,
    DateTime NextDueDate);
