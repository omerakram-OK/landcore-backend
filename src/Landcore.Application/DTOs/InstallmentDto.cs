namespace Landcore.Application.DTOs;

public sealed record InstallmentDto(
    int SeqNo,
    DateTime DueDate,
    decimal Amount,
    string Status,
    decimal PaidAmount);
