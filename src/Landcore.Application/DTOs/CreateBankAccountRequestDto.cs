namespace Landcore.Application.DTOs;

public sealed record CreateBankAccountRequestDto(
    string AccountName,
    string AccountNumber,
    string BankName);
