namespace Landcore.Application.DTOs;

public sealed record CreateInstallmentDto(
    DateTime DueDate,
    decimal Amount);
