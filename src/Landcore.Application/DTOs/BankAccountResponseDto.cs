namespace Landcore.Application.DTOs;

public sealed record BankAccountResponseDto(
    string Id,
    string AdminId,
    string AccountName,
    string AccountNumber,
    string BankName,
    DateTime CreatedAt,
    DateTime UpdatedAt);
