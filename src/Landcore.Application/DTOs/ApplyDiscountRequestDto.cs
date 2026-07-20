namespace Landcore.Application.DTOs;

public sealed record ApplyDiscountRequestDto(
    decimal DiscountAmount,
    string? Notes,
    string? Justification);
