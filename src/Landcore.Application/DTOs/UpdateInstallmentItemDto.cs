namespace Landcore.Application.DTOs;

public sealed record UpdateInstallmentItemDto(
    int? SeqNo,
    DateTime DueDate,
    decimal Amount);
