namespace Landcore.Application.DTOs;

public sealed record BounceChequeRequestDto(
    decimal PenaltyAmount,
    string? Notes);
