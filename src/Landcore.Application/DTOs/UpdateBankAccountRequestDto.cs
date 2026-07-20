namespace Landcore.Application.DTOs;

public sealed record UpdateBankAccountRequestDto(
    string AccountName,
    string AccountNumber,
    string BankName);
