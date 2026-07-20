namespace Landcore.Application.DTOs;

public sealed record RecordPaymentRequestDto(
    string InstallmentPlanId,
    int InstallmentSeqNo,
    decimal Amount,
    string Mode,
    string? BankAccountId,
    DateTime Date,
    string? ChequeNumber,
    string? ChequeBank,
    DateTime? ChequeDueDate,
    DateTime? ChequeDepositDate);
