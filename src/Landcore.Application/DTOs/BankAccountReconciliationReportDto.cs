namespace Landcore.Application.DTOs;

public sealed record BankAccountReconciliationReportDto(
    string BankAccountId,
    string AccountName,
    string AccountNumber,
    string BankName,
    DateTime From,
    DateTime To,
    int TotalPaymentCount,
    decimal TotalRecordedAmount,
    IReadOnlyList<PaymentModeBreakdownDto> ByMode,
    string Note);

public sealed record PaymentModeBreakdownDto(
    string Mode,
    int Count,
    decimal Amount);
