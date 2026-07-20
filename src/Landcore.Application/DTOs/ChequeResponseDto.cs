namespace Landcore.Application.DTOs;

public sealed record ChequeResponseDto(
    string Id,
    string AdminId,
    string PaymentId,
    string ChequeNumber,
    string Bank,
    decimal Amount,
    DateTime DueDate,
    DateTime DepositDate,
    string Status,
    decimal? BouncePenaltyAmount,
    DateTime CreatedAt,
    DateTime UpdatedAt);
