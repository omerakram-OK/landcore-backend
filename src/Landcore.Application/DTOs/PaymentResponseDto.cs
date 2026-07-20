namespace Landcore.Application.DTOs;

public sealed record PaymentResponseDto(
    string Id,
    string AdminId,
    string InstallmentPlanId,
    int InstallmentSeqNo,
    decimal Amount,
    string Mode,
    string? BankAccountId,
    DateTime Date,
    string ReceiptId,
    string? ReceiptNumber,
    decimal? CreditBalanceApplied,
    DateTime CreatedAt);
